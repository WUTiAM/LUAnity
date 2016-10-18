--
-- Lua table 扩展
--

-- 比较两个数组型 table 的元素值是否全相等
function table.isArrayPartValueEqual( at1, at2 )
	local count = #at1
	if count ~= #at2 then 
		return false
	end
	for i = 1, count do
		if at1[i] ~= at2[i] then
			return false
		end
	end
	return true
end

-- 创建只读的 table
function table.readonly( t )
	return setmetatable( {}, {
		__index = t,
		__newindex = function(t, key, value)
			error( "Attempt to modify read-only table!" )
		end,
		__metatable = false,
	} )
end

-- 判断table是否为空, eg: a = {},  table.isEmpty( a )返回true
function table.isEmpty( t )
	local empty = true
	for k, v in pairs( t ) do
		empty = false
		break
	end

	return empty
end

-- 判断table中的value中是否有某值value
function table.hasValue( t, value )
	local flag = false
	for k, v in pairs( t ) do
		if v == value then
			flag = true
			break
		end
	end

	return flag
end

-- 获得table有value的有效长度值,
function table.length( t )
	assert( isTable( t ), "t must be a table" )
	local len = 0
	for _, v in pairs( t ) do
		if v ~= nil then
			len = len + 1
		end
	end

	return len
end

-- 浅拷贝
function table.shallowCopy( t )
	local ct = {}
	for k,v in pairs( t ) do
		ct[k] = v
	end
	return ct
end

-- 深拷贝（打断原有所有 table 引用）
function table.deepCopy( orig )
	-- todo 如果出现环，在debug模式扔出警告
	local origType = type( orig )
	local copy
	if origType == 'table' then
	    copy = {}
	    for k, v in next, orig, nil do
	        copy[table.deepCopy( k )] = table.deepCopy( v )
	    end
	    setmetatable( copy, table.deepCopy( getmetatable( orig ) ) )
	else -- number, string, boolean, etc
	    copy = orig
	end
	return copy
end
