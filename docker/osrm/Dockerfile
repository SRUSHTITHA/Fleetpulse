FROM ghcr.io/project-osrm/osrm-backend:v26.9.0-debian

# The full osrm-backend image ships osrm-extract/partition/customize/routed
# plus the bundled car profile (/opt/car.lua); we only need curl for the
# first-start download of the .osm.pbf region file.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl ca-certificates \
 && rm -rf /var/lib/apt/lists/*

COPY bootstrap.sh /opt/bootstrap.sh
RUN chmod +x /opt/bootstrap.sh

ENTRYPOINT ["/opt/bootstrap.sh"]
