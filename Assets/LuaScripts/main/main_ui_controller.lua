--
-- Main UI Controller
--

local classGOControllerBase = require( "gameobject_controller_base" )
local classVector2D = require( "vector_2d" )

local function classMainUIController()
	local self = classGOControllerBase()

	---- 成员变量 ----

	local _canvasGO = nil

	local _helloTextGO = nil
	local _worldTextGO = nil

	---- 成员函数 前置声明 ----

	local _initButtons
	local _initHelloWorldTexts

	local _onButton1Clicked
	local _onButton2Clicked
	local _onButton3Clicked

	---- 成员函数 ----

	function self.start()
		_canvasGO = L2U.FindGameObject( "Canvas", self.gameObject )

		_initButtons()
		_initHelloWorldTexts()
	end

	function _initButtons()
		local buttonPrototypeGO = L2U.FindGameObject( "Button", _canvasGO )
		buttonPrototypeGO:SetActive( false )

		local buttonLocalPos = buttonPrototypeGO.transform.localPosition
		local buttonLocalPosX = buttonLocalPos.x
		local buttonLocalPosY = buttonLocalPos.y

		local buttonOnClickListeners = {
			_onButton1Clicked,
			_onButton2Clicked,
		}
		local buttonTexts = {
			{ "Hello World", "Control UIs, deferred operations", },
			{ "Add a Tank", "Load prefab, play animation", },
		}

		for i = 1, 2 do
			local buttonGO = L2U.CloneGameObject( buttonPrototypeGO )
			buttonGO:SetActive( true )
			L2U.SetGameObjectLocalPosition( buttonGO, buttonLocalPosX, buttonLocalPosY - 50 * ( i - 1 ), 0 )
			L2U.AddUIButtonOnClickListener( buttonGO, buttonOnClickListeners[i] )

			local buttonText = L2U.FindGameObjectAsComponent( "Text", "Text", buttonGO )
			buttonText.text = buttonTexts[i][1]
			local buttonCommentText = L2U.FindGameObjectAsComponent( "Text_Comment", "Text", buttonGO )
			buttonCommentText.text = buttonTexts[i][2]
		end
	end

	function _initHelloWorldTexts()
		_helloTextGO = L2U.FindGameObject( "HelloWorld/Text_Hello", _canvasGO )
		_helloTextGO:SetActive( false )
		_worldTextGO = L2U.FindGameObject( "HelloWorld/Text_World", _canvasGO )
		_worldTextGO:SetActive( false )
	end

	function _onButton1Clicked( go )
		local button1 = go:GetComponent( "Button" )
		button1.interactable = false

		_helloTextGO:SetActive( true )

		deferredOpSystem.startOp( 0.5, function()
			_worldTextGO:SetActive( true )
		end )

		deferredOpSystem.startOp( 2, function()
			_helloTextGO:SetActive( false )
			_worldTextGO:SetActive( false )

			button1.interactable = true
		end )

		print("Hello world!")
	end

	function _onButton2Clicked( go )
		game.events.tankAdding()
	end

	function self.update()
	end

	function self.onDestroy()
	end

	---- END

	return self
end

return classMainUIController
