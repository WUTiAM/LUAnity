--
-- 用 Lua table 实现的枚举
--

require("type_check")

function enum( tbl, startIndex )
    assert( isTable( tbl ) )

    local enumTbl = {}
    local enumIndex = startIndex or 1

    for i, v in ipairs( tbl ) do
        enumTbl[v] = enumIndex + i - 1
    end

    return table.readonly( enumTbl )
end
