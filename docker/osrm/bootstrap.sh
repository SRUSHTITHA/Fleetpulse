#!/bin/sh
set -eu

DATA_DIR="${DATA_DIR:-/data}"
OUT_BASE="${OUT_BASE:-map}"
PROFILE="${OSRM_PROFILE:-/opt/car.lua}"
PORT="${OSRM_PORT:-5000}"
PBF="${OSRM_PBF_URL:-https://download.geofabrik.de/europe/luxembourg-latest.osm.pbf}"

mkdir -p "$DATA_DIR"
cd "$DATA_DIR"

if [ -f "$DATA_DIR/$OUT_BASE.osrm" ]; then
  echo "[osrm] reusing existing graph at $DATA_DIR/$OUT_BASE.osrm"
else
  echo "[osrm] downloading $PBF ..."
  curl -fL --retry 3 -o "$OUT_BASE.osm.pbf" "$PBF"

  echo "[osrm] extracting (profile: $PROFILE) ..."
  osrm-extract -p "$PROFILE" "$OUT_BASE.osm.pbf"

  echo "[osrm] partitioning ..."
  osrm-partition "$OUT_BASE.osrm"

  echo "[osrm] customizing ..."
  osrm-customize "$OUT_BASE.osrm"

  rm -f "$OUT_BASE.osm.pbf"
  echo "[osrm] graph ready"
fi

echo "[osrm] serving on :$PORT"
exec osrm-routed --algorithm mld --port "$PORT" "$DATA_DIR/$OUT_BASE.osrm"