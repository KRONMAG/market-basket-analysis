local jwt = require "resty.jwt"
local validators = require "resty.jwt-validators"
local http = require "resty.http"
local cjson = require "cjson"

local JWKS_CACHE_KEY = "jwks"
local JWKS_CACHE_LIFESPAN = 3600

local auth_header = ngx.var.http_authorization

if not auth_header then
	ngx.log(ngx.ERR, "Authorization header missing")
	ngx.exit(ngx.HTTP_UNAUTHORIZED)
end

local _, _, token = string.find(auth_header, "Bearer%s+(.+)")

if not token then
	ngx.log(ngx.ERR, "Bearer token missing")
	ngx.exit(ngx.HTTP_UNAUTHORIZED)
end

local cache = ngx.shared.jwks_cache
local jwks = cache:get(JWKS_CACHE_KEY)

if not jwks then
	local httpc = http.new()
	local res, err = httpc:request_uri("$JWKS_URI", {
		method = "GET"
	})
	
	if not res then
		ngx.log(ngx.ERR, "Failed to request JWKS: ", err)
		ngx.exit(ngx.HTTP_INTERNAL_SERVER_ERROR)
	end
	
	if res.status ~= 200 or not res.body then
		ngx.log(ngx.ERR, "Failed to fetch JWKS: ", res)
		ngx.exit(ngx.HTTP_INTERNAL_SERVER_ERROR)
	end
	
	jwks = { }
	local keys = cjson.decode(res.body).keys
	
	for _, key in ipairs(keys) do
		jwks[key.kid] = key
	end

	cache:set(JWKS_CACHE_KEY, jwks, JWKS_CACHE_LIFESPAN)
end

local token_kid = jwt:load_jwt(token).header.kid
local key = jwks[token_kid]

if not key then
	ngx.log(ngx.ERR, "Bearer token KID not found in JWKS: ", token)
	ngx.exit(ngx.HTTP_UNAUTHORIZED)
end

if not key.x5c or not next(key.x5c) then
	ngx.log(ngx.ERR, "Found JWKS item has no certificate chain: ", key)
	ngx.exit(ngx.HTTP_INTERNAL_SERVER_ERROR)
end

if not key.issuer then
	ngx.log(ngx.ERR, "Found JWKS item has no issuer: ", key)
	ngx.exit(ngx.HTTP_INTERNAL_SERVER_ERROR)
end

local cert =
	"-----BEGIN CERTIFICATE-----\n" ..
	key.x5c[1] ..
	"\n-----END CERTIFICATE-----"

local res = jwt:verify(cert, token, {
	exp = validators.is_not_expired(),
	nbf = validators.is_not_before(),
	iss = validators.equals("$JWT_ISSUER"),
	aud = validators.equals("$JWT_AUDIENCE")
})

if not res.verified then
	ngx.log(ngx.ERR, "JWT validation failed: ", res.reason)
	ngx.exit(ngx.HTTP_UNAUTHORIZED)
end