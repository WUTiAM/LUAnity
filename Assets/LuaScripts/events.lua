--
-- 事件组
--

local classEvent = require( "event" )

local function classEvents( events )
    assert( isTable( events ) )

    local t = {}

    for _, eventName in ipairs( events ) do
        t[eventName] = classEvent( eventName )
    end

    return table.readonly( t )
end

return classEvents
