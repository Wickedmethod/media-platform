-- migrations/001_initial_schema.lua
-- Migration: Establish schema version tracking
-- Version: 0 → 1

local version = redis.call('GET', 'platform:schema:version')
if version and tonumber(version) >= 1 then
    return 'SKIP: already at version 1+'
end

-- Set initial schema version
redis.call('SET', 'platform:schema:version', '1')
redis.call('ZADD', 'platform:schema:history', redis.call('TIME')[1], 'v0→v1: initial schema version tracking')

return 'OK: migrated to version 1'
