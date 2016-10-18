--
-- 事件触发类
--

local function classEvent( eventName )
	local self = {}

	assert( eventName ~= nil )

	---- 成员变量 ----

	local _listeners = {}
	local _l = {}

	---- 成员函数 前置声明 ----

	---- 构造器 ----

	---- 成员函数 ----

	function self.addListener( listener )
		if listener == nil then
			error( "Add nil listener to event '" .. eventName .. "'" )
			return
		end
		if _l[listener] ~= nil then
			error( "Add listener to event repeatly " .. eventName )
			return
		end

		_l[listener] = true;

		table.insert( _listeners, listener )
		
		--print( "Add listener to event: " .. eventName )
	end

	function self.removeListener( listener )
		if listener == nil then
			error( "Remove nil listener to event '" .. eventName .. "'" )
			return
		end
		if _l[listener] == nil then
			return
		end

		_l[listener] = nil

		local listenerIndex
		for i, v in ipairs( _listeners ) do
			if listener == v then
				listenerIndex = i
				break
			end
		end

		if listenerIndex ~= nil then
			table.remove(_listeners, listenerIndex)
		else
			error( "don't find listener when removing '" .. eventName .. "'" )
		end
	end

	local function _trigger( self, ... )
		--print( "Trigger event: " .. eventName )

		for _, listener in ipairs( _listeners ) do
			listener( ... )
		end
	end

	---- END

	-- 事件实例可通过 table 直接触发
	setmetatable( self, { __index = self, __call = _trigger } )
	-- 监听列表为弱表，及时释放失效的监听接收函数
	setmetatable( _listeners, { __mode = 'v' } )
	--setmetatable( _l, { __mode = 'k' } )

	return self
end

return classEvent
