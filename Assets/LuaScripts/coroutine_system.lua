--
-- 协同程序系统
--

local function classCoroutineSystem()
	local self = {}

	---- 成员变量 ----

	local _coroutines = {}

	---- 成员函数 前置声明 ----

	---- 构造器 ----

	local function _init()
	end

	---- 成员函数 ----

	function self.startCoroutine( func, ... )
		assert( func ~= nil )

		local co = coroutine.create( func )
		coroutine.resume( co, ... )
		if coroutine.status(co) ~= "dead" then
			_coroutines[co] = true
		end
	end

	-- 不同协同之间 执行顺序不定，如果需要确定 _coroutines改用数组形式
	function self.update()
		for co, _ in pairs( _coroutines ) do
			local success, errorMsg = coroutine.resume( co )
			if not success then
				error( errorMsg )
			end

			if coroutine.status( co ) == "dead" then
				_coroutines[co] = nil
			end
		end
	end

	---- END

	_init()
	
	return self
end

if getGlobal( "coroutineSystem" ) == nil then
	declareGlobal( "coroutineSystem", classCoroutineSystem() )
end

return true
