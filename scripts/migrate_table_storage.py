#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# dependencies = [
#     "azure-data-tables>=12.4.0",
# ]
# ///
"""
Migrate Azure Table Storage entities between storage accounts.

Usage:
    # Set environment variables for security
    export SRC_STORAGE_KEY=<source_key>
    export DST_STORAGE_KEY=<destination_key>
    
    uv run scripts/migrate_table_storage.py \
        --src-account <source_account> \
        --dst-account <destination_account> \
        --table ProcessedPhotos
"""

import argparse
import os
from collections import defaultdict
from collections.abc import Iterator
from dataclasses import dataclass
from azure.data.tables import TableClient, TableServiceClient, TableTransactionError


@dataclass
class MigrationResult:
    migrated: int = 0
    errors: int = 0


def build_connection_string(account: str, key: str) -> str:
    return (
        f"DefaultEndpointsProtocol=https;"
        f"AccountName={account};"
        f"AccountKey={key};"
        f"EndpointSuffix=core.windows.net"
    )


def get_table_client(account: str, key: str, table_name: str) -> TableClient:
    connection_string = build_connection_string(account, key)
    service = TableServiceClient.from_connection_string(connection_string)
    return service.get_table_client(table_name)


def ensure_table_exists(account: str, key: str, table_name: str) -> None:
    connection_string = build_connection_string(account, key)
    service = TableServiceClient.from_connection_string(connection_string)
    try:
        service.create_table(table_name)
        print(f"Created table '{table_name}' in destination")
    except Exception:
        print(f"Table '{table_name}' already exists in destination")


def group_by_partition(entities: list[dict]) -> dict[str, list[dict]]:
    by_partition: dict[str, list[dict]] = defaultdict(list)
    for entity in entities:
        by_partition[entity["PartitionKey"]].append(entity)
    return dict(by_partition)


def batched(items: list, size: int) -> Iterator[list]:
    for i in range(0, len(items), size):
        yield items[i : i + size]


def upsert_batch(
    dst_table: TableClient,
    batch: list[dict],
    partition_key: str,
) -> MigrationResult:
    operations = [("upsert", entity) for entity in batch]
    try:
        dst_table.submit_transaction(operations)
        return MigrationResult(migrated=len(batch))
    except TableTransactionError as e:
        print(f"Batch error in partition '{partition_key}': {e}")
        return upsert_individually(dst_table, batch)


def upsert_individually(dst_table: TableClient, entities: list[dict]) -> MigrationResult:
    result = MigrationResult()
    for entity in entities:
        try:
            dst_table.upsert_entity(entity)
            result.migrated += 1
        except Exception as e:
            print(f"  Failed to insert {entity['RowKey']}: {e}")
            result.errors += 1
    return result


def migrate_entities(
    dst_table: TableClient,
    entities: list[dict],
    batch_size: int,
) -> MigrationResult:
    total = len(entities)
    result = MigrationResult()
    by_partition = group_by_partition(entities)
    print(f"Found {len(by_partition)} partition(s)")

    for partition_key, partition_entities in by_partition.items():
        for batch in batched(partition_entities, batch_size):
            batch_result = upsert_batch(dst_table, batch, partition_key)
            result.migrated += batch_result.migrated
            result.errors += batch_result.errors

            if result.migrated % 500 == 0 or result.migrated + result.errors == total:
                print(f"Progress: {result.migrated}/{total} entities migrated")

    return result


def migrate_table(
    src_account: str,
    src_key: str,
    dst_account: str,
    dst_key: str,
    table_name: str,
    batch_size: int = 100,
) -> None:
    src_table = get_table_client(src_account, src_key, table_name)
    ensure_table_exists(dst_account, dst_key, table_name)
    dst_table = get_table_client(dst_account, dst_key, table_name)

    print(f"Fetching entities from source '{src_account}/{table_name}'...")
    entities = list(src_table.list_entities())
    print(f"Found {len(entities)} entities to migrate")

    if not entities:
        print("Nothing to migrate")
        return

    result = migrate_entities(dst_table, entities, batch_size)

    print("\nMigration complete!")
    print(f"  Migrated: {result.migrated}")
    print(f"  Errors: {result.errors}")


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Migrate Azure Table Storage between accounts"
    )
    parser.add_argument("--src-account", required=True, help="Source storage account name")
    parser.add_argument("--dst-account", required=True, help="Destination storage account name")
    parser.add_argument("--table", default="ProcessedPhotos", help="Table name to migrate")
    parser.add_argument("--batch-size", type=int, default=100, help="Batch size (max 100)")

    args = parser.parse_args()

    # Read storage keys from environment variables for security
    src_key = os.environ.get("SRC_STORAGE_KEY")
    dst_key = os.environ.get("DST_STORAGE_KEY")

    if not src_key:
        parser.error("SRC_STORAGE_KEY environment variable is required")
    if not dst_key:
        parser.error("DST_STORAGE_KEY environment variable is required")

    if args.batch_size > 100:
        print("Warning: Azure limits batch size to 100, using 100")
        args.batch_size = 100

    migrate_table(
        src_account=args.src_account,
        src_key=src_key,
        dst_account=args.dst_account,
        dst_key=dst_key,
        table_name=args.table,
        batch_size=args.batch_size,
    )


if __name__ == "__main__":
    main()
