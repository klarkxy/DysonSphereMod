# 智能物流计划（IntelligentTransport）
稍微优化一点原本惨不忍睹的物流逻辑。
也许，变得更糟？
Slightly optimize the logistics logic that was originally terrible. 
Maybe it gets worse? 

## 前置插件（Front Modules）
- https://github.com/BepInEx/BepInEx

## 实现逻辑（Implementation Logic）
试图把货物送往最近的物流塔，而不是送往最先插下去的物流塔。
Try to send the goods to the nearest logistics tower instead of the one that is created first. 
#### 本地物流
本地物流会根据两个物流塔之间的直线距离（理论上应该用球面距离）进行判定，优先送往就近的物流塔。
Local logistics will be judged according to the straight-line distance between the two logistics towers (theoretically the spherical distance should be used) and will be sent to the nearest logistics tower first.
#### 星际物流
星际物流会根据所在星系恒星的位置距离进行判定，优先送往就近的恒星系（本星系为最优先）。
Remote logistics will be determined according to the position distance of the star in the galaxy, and the priority will be sent to the nearest star system (the local galaxy is the highest priority).
 
## 使用效果（Effects）
### 本地物流（Local Transport）
#### 使用前（Before Use）
![](https://raw.githubusercontent.com/klarkxy/Picture/main/20211006002643.png)
#### 使用后（After Use）
![](https://raw.githubusercontent.com/klarkxy/Picture/main/20211006001952.png)
### 星际物流（Remote Transport）
#### 使用前（Before Use）
![](https://raw.githubusercontent.com/klarkxy/Picture/main/20211006011443.png)
#### 使用后（After Use）
![](https://raw.githubusercontent.com/klarkxy/Picture/main/20211006010223.png)


## 版本（Version）
#### v1.0.0
重构物流运输逻辑。大大降低了排序的计算量。
Reconstruct the logic of logistics and transportation.
#### v0.1.1
完善README，增加英文说明。
Improve the README and add English descriptions. 
#### v0.1.0 
打包上传
Package upload 