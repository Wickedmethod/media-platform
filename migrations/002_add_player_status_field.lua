-- migrations/002_add_player_status_field.lua
-- Migration: Add status field to player registration hashes
-- Version: 1 → 2

local version = redis.call('GET', 'platform:schema:version')
if version and tonumber(version) >= 2 then
    return 'SKIP: already at version 2+'
end

-- Migrate all worker registration hashes
local workers = redis.call('KEYS', 'worker:*')
local migrated = 0
for _, key in ipairs(workers) do
    local keyType = redis.call('TYPE', key)['ok']
    if keyType == 'hash' then
        local hasStatus = redis.call('HEXISTS', key, 'status')
        if hasStatus == 0 then
            redis.call('HSET', key, 'status', 'online')
            migrated = migrated + 1
        end
    end
end

-- Update version
redis.call('SET', 'platform:schema:version', '2')
redis.call('ZADD', 'platform:schema:history', redis.call('TIME')[1], 'v1→v2: add player status field to worker registrations')

return 'OK: migrated to version 2, updated ' .. migrated .. ' worker(s)'
