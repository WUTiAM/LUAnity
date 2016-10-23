--
-- Main Scene Controller
--

local classGOControllerBase = require( "gameobject_controller_base" )

local function classMainController()
	local self = classGOControllerBase()

	---- 成员变量 ----

	---- 成员函数 前置声明 ----

	local _onTankAdding

	---- 成员函数 ----

	function self.awake()
	end

	function self.start()
		self._addEventListener( game.events.tankAdding, _onTankAdding )
	end

	function _onTankAdding()
		AssetLoader.LoadPrefab( "Tank", self.gameObject )

		print("A tank added")
	end

	function self.update()
	end

	function self.onDestroy()
	end

	---- END

	return self
end

return classMainController
