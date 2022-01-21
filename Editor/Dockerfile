FROM ubuntu:latest

RUN apt-get update -qq; \
apt-get install -qq -y curl \
&& apt-get clean \
&& rm -rf /var/lib/apt/lists/*

COPY Server server

RUN useradd -ms /bin/bash unity
RUN chown unity:unity -R server/server.x86_64
RUN chmod +x server/server.x86_64
USER unity


CMD ["./server/server.x86_64", "-batchmode", "-nographics"]