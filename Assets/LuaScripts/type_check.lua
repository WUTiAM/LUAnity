--
-- Lua 数据类型检查
--
function isNumber( o )
	return type( o ) == "number"
end

function isString( o )
	return type( o ) == "string"
end

function isBoolean( o )
	return type( o ) == "boolean"
end

function isTable( o )
	return type( o ) == "table"
end

function isFunction( o )
	return type( o ) == "function"
end

function isUserdata( o )
	return type( o ) == "userdata"
end
