# LUAnity

LUAnity 是一套在 Unity 中使用 Lua 开发手机游戏的解决方案及，并在已上线的 3D 手游大作中有出色的表现。  
LUAnity is a solution for mobile game development using Lua in Unity. It has been verified in onlined 3D mobile games.

LUAnity 基于 LuaInterface，并无缝集成进 Unity，可在 Unity Editor 以及移动设备上运行 Lua 代码来控制游戏。我们大幅修改、重构、整理了 LuaInterface 的代码（同时参考了 [MonoLuaInterface](https://github.com/stevedonovan/MonoLuaInterface) 及 [NLua](https://github.com/NLua/NLua) 的一些修改），并针对手机游戏的需求扩展了一些功能（如支持 ARM64、LuaJIT 等）。  
LUAnity is based on LuaInterface and integrated into Unity seamlessly. It makes Lua codes running perfectly in Unity Editor and mobile devices to control your games. We refactored and optimized the LuaInterface codes (and made a few references to [MonoLuaInterface](https://github.com/stevedonovan/MonoLuaInterface) and [NLua](https://github.com/NLua/NLua)), and added some functions (i.e. ARM64, LuaJIT support) for mobile game development as well.

同时，LUAnity 也是一套代码设计及编写规范。在开发过程始终坚持这些规范，才能真正发挥出 LUAnity 的全部潜力。
In the meantime, LUAnity is also some principles for code design and writing. It's highly recommended to follow these principles to achieve the best results.

---

**特性**：  
**Features**:

- 在 Unity 中无缝编写 Lua/C# 代码，控制所有你想控制的  
Coding in Lua & C# seamlessly in Unity, to control anything you want
- 随时将 C# 中的类/函数引入 Lua，即插即用，无需生成 C# 代码，完美支持热更新  
Importing C# classes/functions to Lua and using them at any time with no C# code generated, best for code updating
- 由原生 Lua 虚拟机执行代码，性能可靠，更可快速替换为原生 LuaJIT  
Running in raw Lua VM with great performance, or even in LuaJIT
- 错误处理及 Lua call stack 追踪  
Handling errors and tracking Lua call stack
- 全面支持 Android/iOS 32位/64位  
Supporting Android and iOS (ARMv7/ARM64)
- 完美对接 uGUI 以及 NGUI 等  
Working perfectly with uGUI, NGUI, and etc

**要求**：  
**Requires**:

- Lua 5.1.x or higher (5.1.5 by default)
 - or LuaJIT (2.0.3 for Android and 2.1.0 for iOS)
- Unity 4.6 or higher (5.4 by default)

**支持平台**：  
**Suported Platforms**:

- Android (ARMv7/ARM64)
- iOS (ARMv7/ARM64)

---

**为什么我们选择基于反射的动态绑定方式（而不是静态绑定）？**  
**Why do we prefer dynamic binding (using reflection) rather than static binding?**

我们在 Unity 中使用 Lua 的核心目标，就是让尽可能多的代码支持热更新，而不需要重新出包重新发布游戏。  

基于反射（也就是 LuaInterface 使用的绑定方式），可以最大程度的降低修改 C# 代码的频率及代码量。在开发过程中，随时随地将 Unity/Mono 的类引入 Lua 来使用，并立即将新功能热更新到线上环境，想想就有点小激动，对吧。  

这是静态绑定所无法提供的。静态绑定的方式需要为所有 Lua 可能会使用到的 Unity/Mono 类和函数生成 C# 绑定代码(而 C# 代码的改动需要重新出包重新发布游戏)这很大程度上限制了热更新的应用场景。  

那么，说到反射，大家最关心的就是性能问题了。  

我们一直相信，游戏性能的好坏，主要取决于制作方法和对技术的使用方式。以什么样的姿势使用 LUAnity，开发出来的游戏性能可以相差几十倍。  

也就是说，如何定义 Unity 和 Lua 的关系、如何设计游戏框架、如何制定代码规范并坚持执行，才是决定游戏性能是否达标的主要因素。  

我们的设计规范其实很简单：  

1. 在 C# 层仅提供很少会改动的**基础**功能支持和接口  
2. 所有业务逻辑**全部**在 Lua 内部实现  
 * 甚至包括策划数据表和 Protocol Buffer  
3. 严禁在一帧内**高频**来回于 Lua 和 C#  

我们的 Lua 编码规范也很简单，就一条：

1. 基于闭包的面向对象
 * 拥有真正的私有成员
  * 不会被其他人乱改乱用
 * 访问私有成员速度更快
  * 直接 upvalue 访问，无需 metatable 查表
 * 函数调用速度更快
  * 不用从 __index 元方法中查询

性能问题，关键还是看实施。坚持执行这套规范，做好核心代码审查，我们基于 LUAnity 的 3D 手游在低配机上依然可以顺畅游戏。  

大家常用的简单粗暴循环100万次的性能测试确实能**部分**反映框架实现的优劣，但游戏是一个复杂的逻辑综合体，有太多的因素影响运行性能，比如一个好的缓存设计能大幅降低频繁从 Lua 访问 C# 数据的巨额开销。  

“反射太慢，静态绑定才够快”，脱离了应用场景人云亦云，就是赤果果地在缴智商税。  

---

以下是这几年出现过的 Unity + Lua 解决方案，各有特点，仅供参考  
The following are some Unity + Lua solutions in the last few years, FYI

- [NLua](https://github.com/NLua/NLua) 
 - LuaInterface 停止维护后原作者推荐的正统分支，需要自行解决和 Unity 的整合工作。
 - 有 [Unity3D-NLua](https://github.com/Mervill/Unity3D-NLua) 及 [NLua for Unity](https://www.assetstore.unity3d.com/cn/#!/content/17389) 等第三方整合方案，但都早已停止维护。
- [uLua](https://www.assetstore.unity3d.com/en/#!/content/13887)
 - 可能是最早的的 Unity + Lua 解决方案，同样基于 LuaInterface，特色是支持 LuaJIT，已停止维护。
 - uLua 对 LuaInterface 对接到 Unity 的包装较为简单，整合到产品开发中还需要做很多工作，而且缺少文档，修改及整合都比较费劲。
- [uLua.org 版本的 uLua](http://ulua.org/download.html)
 - 可能是国内最早[分享 uLua 使用经验](http://www.ceeger.com/forum/read.php?tid=16483)的团队，并维护了国内版的 uLua（即 v1.08 版及以后）。
 - 后来放弃了 LuaInterface 家族基于反射的实现机制，转向开发并使用 CsToLua。
- [CStoLua](https://github.com/topameng/CsToLua) / [toLua#](https://github.com/topameng/tolua)
 - 类似 tolua++，基于静态绑定的 Unity/C# + Lua 方案解决
- [Slua](https://github.com/pangweiwei/slua)
 - 另一个基于静态绑定的 Unity/C# + Lua 方案解决，和 tolua# 打得难解难分 :p
