--
-- 禁止隐式声明、访问全局变量
--

function declareGlobal( name, initValue )
	rawset( _G, name, initValue or false )
end

function getGlobal( name )
	return rawget( _G, name )
end

setmetatable( _G, {
	__newindex = function ( _, n )
		error( "Attempt to write to undeclared global variable: " .. n )
	end,
	__index = function (_, n)
		error( "Attempt to read undeclared global variable: " .. n )
	end,
} )
