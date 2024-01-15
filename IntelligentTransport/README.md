# 智能物流计划
稍微优化一点原本的物流逻辑。
也许，变得更糟？

## 前置插件
- https://dsp.thunderstore.io/package/xiaoye97/BepInEx/
- https://dsp.thunderstore.io/package/CommonAPI/DSPModSave/

## 实现逻辑
试图把货物送往最近、优先级更高的物流塔，而不是送往最先插下去的物流塔。 
#### 本地物流
1. 如果两个物流塔的优先级不一致，则优先送往优先级更高的物流塔。
2. 如果两个物流塔的优先级一致，则优先送往距离更近的物流塔。
3. 如果两个物流塔的优先级一致且距离相同，则优先送往最先插下去的物流塔。
#### 星际物流
1. 如果两个物流塔所在的恒星系不一致，则优先送往距离更近的恒星系。
2. 如果两个物流塔所在的恒星系一致，则优先送往优先级更高的物流塔。
3. 如果两个物流塔所在的恒星系一致且优先级相同，则优先送往最先插下去的物流塔。
#### 气体采集器和大矿机
默认气体采集器、大矿机的优先级为0，即当所有的物流塔都没有该物品时，才会去其中取。
 
## 版本（Version）
#### v1.1.0
增加优先级设置，但是目前这个优先级设置存在性能问题。
#### v1.0.0
重构物流运输逻辑。大大降低了排序的计算量。
#### v0.1.1
完善README，增加英文说明。
#### v0.1.0 
打包上传

# Intelligent Logistics Program
Slightly optimize the original logistics logic.
Perhaps, become worse?

## Pre-plugins
- https://dsp.thunderstore.io/package/xiaoye97/BepInEx/
- https://dsp.thunderstore.io/package/CommonAPI/DSPModSave/

## Implement the logic
Attempts to send the shipment to the nearest, higher priority logistics tower, rather than to the first one plugged down. 
#### Local Logistics
1. if two logistics towers do not have the same priority, send first to the higher priority logistics tower.
2. if the priority of the two towers is the same, then priority is given to the closer tower.
3. if two towers have the same priority and are the same distance apart, then the first tower to be plugged in will be given priority.
#### Interstellar Logistics
1. if the two Logistics Towers are not in the same star system, priority is given to the closer star system. 2. if the two Logistics Towers are not in the same star system, priority is given to the closer star system.
2. if two Logistics Towers are in the same star system, then the Tower with the higher priority is sent first. 3.
3. if the two logistics towers are in the same star system and have the same priority, then priority is given to the first logistics tower to be plugged in.
#### Gas Collectors and Big Miners
By default, gas collectors and big miners have priority 0, i.e., they will only go to one of the logistics towers when all of them do not have the item.
 
## Version
#### v1.1.0
Add priority setting, but currently there are performance issues with this priority setting.
#### v1.0.0
Refactored logistics and transportation logic. Greatly reduced sorting calculations.
#### v0.1.1
Improve README and add English description.
#### v0.1.0 
Packing and uploading
