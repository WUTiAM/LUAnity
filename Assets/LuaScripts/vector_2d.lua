--
-- 2D 向量
--

local classVector2D

local function _equals( lhsE, rhsE )
	return math.abs( lhsE[1] - rhsE[1] ) <= NUMBER_EC and math.abs( lhsE[2] - rhsE[2] ) <= NUMBER_EC
end

local function _add( lhsA, rhsA )
	return classVector2D( lhsA[1] + rhsA[1], lhsA[2] + rhsA[2] )
end

local function _sub( lhsS, rhsS )
	return classVector2D( lhsS[1] - rhsS[1], lhsS[2] - rhsS[2] )
end

local function _mul( lhsM, scalar )
	return classVector2D( lhsM[1] * scalar, lhsM[2] * scalar )
end

local _mt = {
	__eq  = _equals,
	__add = _add,
	__sub = _sub,
	__mul = _mul,
}

function classVector2D( x, y )
	local self = { x or 0, y or 0 }

	---- 成员变量 ----

	---- 成员函数 前置声明 ----

	---- 成员函数 ----

	-- ！注意！
	-- 传值赋值一定要使用 assign() 函数！ 
	-- “=”方式仅仅是引！用！！
	function self.assign( rhs )
		self[1] = rhs[1]
		self[2] = rhs[2]
	end

	function self.isZero()
		return self[1] == 0 and self[2] == 0
	end

	function self.lengthSq()
		return self[1]^2 + self[2]^2
	end

	function self.length()
		return math.sqrt( self.lengthSq() )
	end

	function self.distanceSq( rhs )
		return ( rhs[1] - self[1] )^2 + ( rhs[2] - self[2] )^2
	end

	function self.distance( rhs )
		return math.sqrt( self.distanceSq( rhs ) )
	end

	function self.dot( rhs )
		return ( self[1] * rhs[1] + self[2] * rhs[2] )
	end

	function self.cross( rhs )  
		return ( self[1] * rhs[2] - self[2] * rhs[1] )
	end

	function self.normalize()
		local len = self.length()
		if len > 0 then
			self[1] = self[1] / len
			self[2] = self[2] / len
		end
	end

	function self.lerp( from, to, t )
		t = math.max( math.min( t, 1 ), 0 )

		local x = from[1]
		self[1] = x + ( to[1] - x ) * t
		local y = from[2]
		self[2] = y + ( to[2] - y ) * t
	end

	---- END

	setmetatable( self, _mt )
	return self
end

return classVector2D
