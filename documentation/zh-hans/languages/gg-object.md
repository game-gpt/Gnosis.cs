# ggon 语言语法指南

本文档介绍 ggon 语言的语法特性和使用方法。

## 语言概述

ggon 是 gg 引擎的对象配置语言，具有以下特性：

| 特性          | 描述                |
| :---------- | :---------------- |
| 类名可选        | 支持 `ClassName { xxx }` 形式 |
| 变体必选        | 强制使用 `VariantName { xxx }` 形式 |
| 字段无引号       | 字段名不需要引号包围       |
| 多值类型支持      | 支持 string, integer, decimal, boolean, null |
| 与 JSON 兼容     | 可作为 JSON 的超集使用     |

## 基础语法

### 基本结构

```ggon
# 带类名的对象
Player {
    name: "John",
    level: 10,
    position: {
        x: 100.0,
        y: 200.0
    }
}

# 不带类名的对象（与 JSON 类似）
{
    name: "John",
    level: 10
}

# 带变体的对象（变体必选）
Position2D {
    x: 100.0,
    y: 200.0
}

Position3D {
    x: 100.0,
    y: 200.0,
    z: 300.0
}
```

### 字段定义

字段名不需要引号包围，直接使用标识符：

```ggon
# 正确的字段定义
{
    name: "John",
    level: 10,
    is_active: true
}

# 也支持引号包围（与 JSON 兼容）
{
    "name": "John",
    "level": 10
}
```

## 类型系统

### 值类型

| 类型          | 描述                | 示例                |
| :---------- | :---------------- | :---------------- |
| `string`    | 字符串              | `"Hello"`         |
| `i32`       | 32位整数            | `42`               |
| `u32`       | 32位无符号整数        | `42`               |
| `f32`       | 32位浮点数           | `3.14`             |
| `f64`       | 64位浮点数           | `3.1415926535`     |
| `boolean`   | 布尔值              | `true`, `false`    |
| `null`      | 空值               | `null`             |
| `object`    | 对象               | `{ name: "John" }` |
| `array`     | 数组               | `[1, 2, 3]`        |

### 类型示例

```ggon
{
    // 字符串
    name: "John Doe",
    
    // 整数 (i32)
    age: 30,
    score: 95,
    
    // 小数 (f32)
    height: 1.75,
    weight: 68.5,
    
    // 布尔值
    is_active: true,
    is_admin: false,
    
    // 空值
    avatar: null,
    
    // 对象
    address: {
        street: "Main St",
        city: "New York"
    },
    
    // 数组
    hobbies: ["reading", "gaming", "coding"],
    scores: [95, 88, 92]
}
```

## 类和变体定义

### 类定义（可选）

类名是可选的，用于标识对象的类型：

```ggon
# 带类名的对象
Player {
    name: "John",
    level: 10
}

# 不带类名的对象
{
    name: "John",
    level: 10
}
```

### 变体定义（必选）

变体名是必选的，用于区分不同类型的对象：

```ggon
# 位置变体
Position2D {
    x: 100.0,
    y: 200.0
}

Position3D {
    x: 100.0,
    y: 200.0,
    z: 300.0
}

# 形状变体
Circle {
    radius: 5.0
}

Rectangle {
    width: 10.0,
    height: 20.0
}
```

### 类与变体的组合

类和变体可以组合使用：

```ggon
# 带类名和变体的对象
GameEntity Position3D {
    x: 100.0,
    y: 200.0,
    z: 300.0,
    name: "Player"
}

# 带类名的多个变体
Character {
    Human {
        name: "John",
        race: "Human"
    }
    Elf {
        name: "Legolas",
        race: "Elf",
        has_ears: true
    }
}
```

## 最佳实践

### 代码风格

- 使用缩进（2或4个空格）提高可读性
- 字段名使用小写蛇形命名法（snake_case）
- 变体名使用 PascalCase 命名法
- 类名使用 PascalCase 命名法

```ggon
# 良好的代码风格
Player {
    name: "John",
    level: 10,
    position: Position3D {
        x: 100.0,
        y: 200.0,
        z: 300.0
    }
}
```

### 常见错误

- 变体名缺失（变体是必选的）
- 字段名使用特殊字符而不加引号
- 数组和对象末尾多余的逗号

```ggon
# 错误：缺少变体名
{
    x: 100.0,
    y: 200.0
}

# 错误：字段名包含特殊字符但未加引号
{
    user-name: "John" # 应该使用 "user-name": "John"
}
```

### 实际示例

#### 游戏配置

```ggon
GameConfig {
    player: Player {
        name: "Hero",
        level: 1,
        stats: {
            health: 100,
            mana: 50,
            strength: 10
        }
    },
    enemies: [
        Enemy Goblin {
            name: "Goblin",
            level: 1,
            health: 50
        },
        Enemy Orc {
            name: "Orc",
            level: 2,
            health: 80
        }
    ]
}
```

#### UI 配置

```ggon
UIConfig {
    main_menu: Panel {
        position: Position2D {
            x: 0,
            y: 0
        },
        size: Size {
            width: 800,
            height: 600
        },
        children: [
            Button {
                text: "Start Game",
                position: Position2D {
                    x: 350,
                    y: 200
                }
            },
            Button {
                text: "Options",
                position: Position2D {
                    x: 350,
                    y: 250
                }
            }
        ]
    }
}
```

## 与引擎集成

### 加载 ggon 文件

在 gg 引擎中，可以使用 `asset.load` 函数加载 ggon 配置文件：

```tsx
# 加载游戏配置
let game_config = asset.load<GonObject>("configs/game.ggon");

# 访问配置数据
let player_name = game_config.get("player").get("name").as_string();
let player_level = game_config.get("player").get("level").as_int();
```

### 类型安全访问

使用类型转换确保类型安全：

```tsx
# 安全访问配置
let player = game_config.get("player");
if player.is_object() {
    let stats = player.get("stats");
    let health = stats.get("health").as_int();
    let mana = stats.get("mana").as_int();
}
```

### 变体类型处理

处理不同变体类型的对象：

```tsx
# 处理位置变体
let position = game_config.get("player").get("position");
if position.variant_name() == "Position2D" {
    let x = position.get("x").as_float();
    let y = position.get("y").as_float();
} else if position.variant_name() == "Position3D" {
    let x = position.get("x").as_float();
    let y = position.get("y").as_float();
    let z = position.get("z").as_float();
}
```

### 实时重载

编辑器支持 ggon 文件的热重载，修改配置文件后会自动重新加载：

```tsx
# 监听配置变化
asset.watch("configs/game.ggon", micro(new_config) {
    console::log("配置已更新:", new_config.get("player").get("name").as_string());
});
```

## 下一步

- 阅读 [资源系统](../development/architecture.md) 了解资源加载机制
- 查看 [示例项目](../../examples/) 了解实际用法
