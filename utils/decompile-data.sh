#!/bin/sh
FILE="$1"
shift
mono ./altar.exe export --any -k --file "$FILE" --project --detachedagrp "$@"
