FROM fabiocicerchia/nginx-lua:1.27.1-alpine3.20.2 AS build

RUN apk add --no-cache build-base
RUN luarocks install lua-resty-jwt
RUN luarocks install lua-resty-http
RUN luarocks install lua-cjson

FROM fabiocicerchia/nginx-lua:1.27.1-alpine3.20.2 AS final

RUN rm -rf /usr/local
COPY --from=build /usr/local /usr/local

ENTRYPOINT ["nginx", "-g", "daemon off;"]