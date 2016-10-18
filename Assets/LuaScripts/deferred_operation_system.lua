--
-- 延时执行系统
--

local function classDeferredOpSystem()
	local self = {}

	---- 成员变量 ----

	local _ops = {}

	---- 成员函数 前置声明 ----

	---- 构造器 ----

	local function _init()
	end

	---- 成员函数 ----

	-- 这里非常诡异的是，uLua 处理传入可变参数居然是 userdata 类型的，而不是 table，
	-- 而且没有隐含的 arg 变量用于取“...”的参数列表，
	-- 所以只能用固定参数个数来凑合表达了 >_<
	function self.startOp( deferredSeconds, func, p1, p2, p3, p4 )
		assert( deferredSeconds >= 0 )
		assert( func ~= nil )

		if deferredSeconds == 0 then
			func( p1, p2, p3, p4 )
		else
			local op = {
				deferredSeconds = deferredSeconds,
				execute = function() return func( p1, p2, p3, p4 ) end,
			}
			_ops[op] = game.time
			-- print( "Add deferred op " .. deferredSeconds )

			return op
		end
	end

	function self.cancelOp( op )
		if op ~= nil then
			_ops[op] = nil
		end
	end

	function self.doOpImmediately( op )
		if _ops[op] ~= nil then
			op.execute()

			_ops[op] = nil
		end
	end

	function self.update()
		for op, opStartTime in pairs( _ops ) do
			local gameTime = game.time
			if gameTime - opStartTime >= op.deferredSeconds then
				-- 延时到了就触发操作
				_ops[op] = nil
				op.execute()
			end
		end
	end

	---- END

	_init()
	
	return self
end

if getGlobal( "deferredOpSystem" ) == nil then
	declareGlobal( "deferredOpSystem", classDeferredOpSystem() )
end

return true
