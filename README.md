# LUAnity

LUAnity 是一套在 Unity 中使用 Lua 的手机游戏开发解决方案，并在已上线的 3D 手游大作中有出色的表现。  
LUAnity is a solution for mobile game development using Lua in Unity. It has been verified in onlined 3D mobile games.

LUAnity 基于 LuaInterface，使用反射机制来实现 Lua 对 C# 的访问控制, 并参考了 [MonoLuaInterface](https://github.com/stevedonovan/MonoLuaInterface) 及 [NLua](https://github.com/NLua/NLua) 的部分修改。同时，LUAnity 扩展了一些功能（如支持 ARM64、Protocol Buffers、JSON 等），。  
LUAnity is derived from LuaInterface based on reflection for Lua to C#, and made a few references to the modifications of [MonoLuaInterface](https://github.com/stevedonovan/MonoLuaInterface) and [NLua](https://github.com/NLua/NLua). LUAnity added a few functions (i.e. ARM64, Protocol Buffers, JSON support).

***

特性：  
Features:
- 在 Unity 中无缝编写 Lua/C# 代码，控制所有你想控制的 / Coding in Lua & C# seamlessly in Unity, to control everything you want
- 代码支持随时热更新，无需依赖 C# 代码改动 / Code updating at anytime with NO binding C# code generated
- 协程、错误处理等更多功能支持 / Supporting Coroutine, error handling, etc
- 全面支持 Android/iOS 32位/64位 / Supporting Android and iOS (32/64bit)

要求：  
Requires:
- Lua 5.1.4 or higher (5.1.4 by default)
 - or LuaJIT (2.0.3 for Android and 2.1.0 for iOS)
- Unity 4.6 or higher (5.4 by default)

支持平台：  
Suported Platforms:
- Android (32bit/64bit)
- iOS (32bit/64bit)

已集成第三方组件：  
Third-Party Module Integration:
- [dkjson](http://dkolf.de/src/dkjson-lua.fsl/home)
- [Protocol Buffers](https://github.com/google/protobuf)
- [protoc-gen-lua](https://github.com/paynechu/protoc-gen-lua)

***

以下是这几年出现过的 Unity + Lua 解决方案，各有特点，仅供参考 
The following are some Unity + Lua solutions in the last few years, FYI

- [NLua](https://github.com/NLua/NLua) 
 - LuaInterface 停止维护后原作者推荐的正统分支，需要自行解决和 Unity 的整合工作。
 - 有 [Unity3D-NLua](https://github.com/Mervill/Unity3D-NLua) 及 [NLua for Unity](https://www.assetstore.unity3d.com/cn/#!/content/17389) 等第三方整合方案，但都早已停止维护。
- [uLua](https://www.assetstore.unity3d.com/en/#!/content/13887)
 - 可能是最早的的 Unity + Lua 解决方案，同样基于 LuaInterface，特色是支持 LuaJIT，已停止维护。
 - uLua 对 LuaInterface 对接到 Unity 的包装较为简单，整合到产品开发中还需要做很多工作，而且缺少文档，修改及整合都比较费劲。
- [uLua.org 版本的 uLua](http://ulua.org/download.html)
 - 可能是国内最早[分享 uLua 使用经验](http://www.ceeger.com/forum/read.php?tid=16483)的团队，并维护了国内版的 uLua（v1.08 版及以后）。
 - 后来放弃了 LuaInterface 家族基于反射的实现机制，转向开发并使用 CsToLua。
- [CStoLua](https://github.com/topameng/CsToLua) / [toLua#](https://github.com/topameng/tolua)
 - 类似 tolua++，基于静态绑定的 Unity/C# + Lua 方案解决
- [Slua](https://github.com/pangweiwei/slua)
 - 另一个基于静态绑定的 Unity/C# + Lua 方案解决，和 tolua# 打得难解难分 :p
