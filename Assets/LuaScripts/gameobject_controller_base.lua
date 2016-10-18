--
-- 控制器基类
--

local function classGOControllerBase()
	local self = {}

	---- 成员变量 ----

	self.gameObject = nil

	local _eventListeners = {}

	---- 成员函数 前置声明 ----

	local function _init()
	end

	---- 成员函数 ----

	function self.setGameObject(go)
		self.gameObject = go
	end

	function self._addEventListener( event, listener )
		assert( event ~= nil and listener ~= nil )
		assert( _eventListeners[event] == nil )

		_eventListeners[event] = listener
		event.addListener( listener )
	end

	function self._removeEventListener( event, listener )
		assert( event ~= nil and listener ~= nil )

		_eventListeners[event] = nil
		event.removeListener( listener )
	end

	function self._removeAllEventListeners()
		for event, listener in ipairs( _eventListeners ) do
			event.removeListener( listener )
		end
	end

	-- C# 调 Lua 端的 onDestroy 做特殊处理，用于统一收尾处理
	-- Lua 端本身继续使用 self.onDestroy() 作为实现实体
	function self.__onDestroy()
		self._removeAllEventListeners()

		if self.onDestroy ~= nil then
			self.onDestroy()
		end

		self.gameObject = nil
	end

	---- END

	_init()

	return self
end

return classGOControllerBase
