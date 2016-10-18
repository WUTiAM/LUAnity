--
-- Lua string 扩展
--

-- 删除字符串头尾的空白字符
function string.trim( str )
	return str:match( "^%s*(.-)%s*$" )
end

-- 以指定模式分割字符串
function string.split( str, pattern )
	local startIndex = 1
	local flagStartIndex = 1
	local flagEndIndex = 0
	local result = {}	

	repeat
		startIndex = flagEndIndex + 1

		flagStartIndex, flagEndIndex = string.find( str, pattern, startIndex, true )
		local segment = string.sub( str, startIndex, flagStartIndex and flagStartIndex -1 or nil )
		table.insert( result, segment )
	until flagStartIndex == nil

	return result
end
