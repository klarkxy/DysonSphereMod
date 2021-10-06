# 虚空矿机（VoidMiner）
让你的矿机可以采自己范围之外的矿脉。

Allow your mining machine to mine veins outside of your own range. 
**目前可以通过成就检测。**
**Currently it can pass the achievement test.** 

## 前置插件（Front Modules）
- https://github.com/BepInEx/BepInEx

## 实现逻辑（Implementation Logic）
在矿机对自己范围内的矿脉开采之前，会在同星球寻找若干同种矿脉进行开采。数量可以配置。
*当矿机内存货超过45个后失效*

Before the mining machine mines the veins within its own range, it will search for several veins of the same type on the same planet for mining. The number can be configured.
*When the miner's memory exceeds 45 goods, it will become invalid* 

## 版本（Version）
#### v0.1.4
修复因为虚空采矿导致游戏本身自带的采矿逻辑出错的BUG。
修复抽水机无效的BUG。

Fix the bug that caused the mining logic of the game itself to be wrong due to the void mining.
Fix the bug that the pump is invalid. 

Delete the newline character in the configuration file. 
#### v0.1.3
删除配置文件里的换行符。

Delete the newline character in the configuration file. 
#### v0.1.2
修改虚空挖矿的逻辑，由玩家自行控制每次挖矿获得的数量和两次挖矿之间的间隔。

Modify the logic of void mining so that the player controls the amount of each mining and the interval between two mining. 
#### v0.1.1
修复一个数组越界造成的报错。

Fix an error caused by an array out of bounds. 

#### v0.1.0 
打包上传。

Package upload. 