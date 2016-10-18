--
-- Main UI Controller
--

local classGOControllerBase = require( "gameobject_controller_base" )
local classVector2D = require( "vector_2d" )

local function classMainUIController()
	local self = classGOControllerBase()

	---- 成员变量 ----

	local _BUTTON_TEXTS = {
		"Hello World",
		"Play with 3D Model",
		"Lougout"
	}

	---- 成员函数 前置声明 ----

	---- 成员函数 ----

	function self.awake()
		buttonPrototypeGO = L2U.FindGameObject( "Canvas/Button", self.gameObject )
		buttonPrototypeGO:SetActive( false )

		local buttonGO1 = L2U.CloneGameObject( buttonPrototypeGO )
		buttonGO1:SetActive( true )
		local button1 = buttonGO1:GetComponent( "Button" )
		local buttonText1 = L2U.FindGameObjectAsComponent( "Text", "Text", buttonGO1 )
		buttonText1.text = "Hello World"
	end

	function self.start()
		-- self._addEventListener( game.events.logout, _onBattleSynced )

		-- for i = 1, BATTLE_IN_HAND_CARD_MAX_COUNT do
		-- 	local cardGO = _currentCardsInfo[i].go

		-- 	L2U.AddUIButtonDragStartEventHandler( cardGO, _onCardDragStarted )
		-- 	L2U.AddUIButtonDragEventHandler( cardGO, _onCardDragging )
		-- 	L2U.AddUIButtonDragEndEventHandler( cardGO, _onCardDragEnded )
		-- end

		-- _onBattleSynced()
	end

	function self.update()
	end

	function self.onDestroy()
	end

	---- END

	return self
end

return classMainUIController
