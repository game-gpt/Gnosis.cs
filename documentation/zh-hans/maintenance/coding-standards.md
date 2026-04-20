# Rust 编码规范

本文档定义了 Gnosis 引擎项目的 Rust 编码规范。

## 代码风格

### 格式化

使用 `rustfmt` 进行代码格式化：

```bash
cargo fmt
```

### 命名规范

| 类型 | 规范 | 示例 |
| :--- | :--- | :--- |
| 模块 | snake_case | `bullet_pattern` |
| 类型 | PascalCase | `BulletPattern` |
| 函数 | snake_case | `create_bullet` |
| 变量 | snake_case | `bullet_count` |
| 常量 | SCREAMING_SNAKE_CASE | `MAX_BULLETS` |
| 生命周期 | 单引号小写 | `'a`, `'lifetime` |

### 文件组织

```rust
// 1. 外部依赖
use std::collections::HashMap;
use serde::{Deserialize, Serialize};

// 2. 内部模块
use crate::components::*;
use crate::systems::*;

// 3. 类型定义
pub struct MyStruct {
    // ...
}

// 4. 实现
impl MyStruct {
    // ...
}

// 5. 函数
pub fn my_function() {
    // ...
}
```

## 文档注释

### 模块文档

```rust
//! # 弹幕模式模块
//!
//! 本模块提供各种弹幕发射模式的定义和实现。
//!
//! ## 支持的模式
//!
//! - 直线弹幕
//! - 扇形弹幕
//! - 圆形弹幕
//! - 螺旋弹幕
```

### 函数文档

```rust
/// 创建新的弹幕发射器。
///
/// # Arguments
///
/// * `pattern` - 弹幕模式
/// * `spawn_rate` - 生成速率（每秒）
///
/// # Returns
///
/// 返回配置好的弹幕发射器实例。
///
/// # Example
///
/// ```
/// let emitter = BulletEmitter::new(BulletPattern::Circle { count: 32 }, 10.0);
/// ```
pub fn new(pattern: BulletPattern, spawn_rate: f32) -> Self {
    // ...
}
```

### 安全性文档

```rust
/// # Safety
///
/// 调用者必须确保 `ptr` 指向有效的内存区域。
pub unsafe fn from_ptr(ptr: *const u8) -> Self {
    // ...
}
```

## 错误处理

### Result 类型

```rust
pub fn parse_script(source: &str) -> Result<Script, ParseError> {
    // ...
}
```

### 自定义错误类型

```rust
#[derive(Debug, thiserror::Error)]
pub enum ParseError {
    #[error("语法错误: {message} at line {line}")]
    SyntaxError { message: String, line: usize },
    
    #[error("未知指令: {0}")]
    UnknownCommand(String),
    
    #[error("IO 错误: {0}")]
    Io(#[from] std::io::Error),
}
```

### 错误传播

```rust
pub fn load_game(path: &Path) -> Result<Game, Error> {
    let config = fs::read_to_string(path.join("game.toml"))?;
    let game: GameConfig = toml::from_str(&config)?;
    Ok(Game::from_config(game))
}
```

## 类型设计

### 结构体

```rust
/// 弹幕发射器组件。
#[derive(Debug, Clone)]
pub struct BulletEmitter {
    /// 弹幕模式
    pub pattern: BulletPattern,
    /// 生成速率（每秒）
    pub spawn_rate: f32,
    /// 每次生成的子弹数量
    pub spawn_count: u32,
    /// 是否继承发射者速度
    pub inherit_velocity: bool,
}
```

### 枚举

```rust
/// 弹幕模式类型。
#[derive(Debug, Clone, Copy)]
pub enum BulletPattern {
    /// 直线弹幕
    Straight { angle: f32 },
    /// 扇形弹幕
    Spread { start_angle: f32, end_angle: f32, count: u32 },
    /// 圆形弹幕
    Circle { count: u32 },
    /// 螺旋弹幕
    Spiral { angular_speed: f32, radial_speed: f32 },
}
```

### 泛型

```rust
/// 资源句柄。
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub struct Handle<T> {
    id: u32,
    _marker: PhantomData<T>,
}
```

## 性能考量

### 避免不必要的克隆

```rust
// 不推荐
fn process(data: Vec<u8>) {
    let cloned = data.clone();
    // ...
}

// 推荐
fn process(data: &[u8]) {
    // ...
}
```

### 使用迭代器

```rust
// 不推荐
let mut sum = 0;
for i in 0..items.len() {
    sum += items[i];
}

// 推荐
let sum: i32 = items.iter().sum();
```

### 避免过度分配

```rust
// 不推荐
let mut vec = Vec::new();
for i in 0..1000 {
    vec.push(i);
}

// 推荐
let vec: Vec<i32> = (0..1000).collect();
// 或
let mut vec = Vec::with_capacity(1000);
```

## 测试规范

### 单元测试

```rust
#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_bullet_emitter_creation() {
        let emitter = BulletEmitter::new(BulletPattern::Circle { count: 32 }, 10.0);
        assert_eq!(emitter.spawn_rate, 10.0);
    }

    #[test]
    fn test_parse_error() {
        let result = parse_script("invalid {{{");
        assert!(result.is_err());
    }
}
```

### 集成测试

集成测试放在 `tests/` 目录下：

```rust
// tests/integration_test.rs
use gg_engine_stg::engine::StgEngine;

#[test]
fn test_engine_initialization() {
    let engine = StgEngine::default();
    assert!(engine.initialize().is_ok());
}
```

## Clippy 规则

项目使用 Clippy 进行代码质量检查：

```bash
cargo clippy -- -D warnings
```

### 常见 Clippy 警告处理

```rust
// 允许特定警告
#[allow(clippy::too_many_arguments)]
pub fn complex_function(a: i32, b: i32, c: i32, d: i32, e: i32) {
    // ...
}
```

## 依赖管理

### 添加依赖

在 `Cargo.toml` 中添加：

```toml
[dependencies]
serde = { version = "1.0", features = ["derive"] }
thiserror = "1.0"
```

### 开发依赖

```toml
[dev-dependencies]
criterion = "0.5"
```

## 最佳实践

### 避免 unwrap

```rust
// 不推荐
let value = option.unwrap();

// 推荐
let value = option.expect("必须提供有效值");
// 或
if let Some(value) = option {
    // ...
}
```

### 使用 newtype 模式

```rust
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct PlayerId(u32);

impl PlayerId {
    pub fn new(id: u32) -> Self {
        Self(id)
    }
    
    pub fn as_u32(&self) -> u32 {
        self.0
    }
}
```

### 使用 Builder 模式

```rust
pub struct BulletEmitterBuilder {
    pattern: Option<BulletPattern>,
    spawn_rate: f32,
    spawn_count: u32,
}

impl BulletEmitterBuilder {
    pub fn new() -> Self {
        Self {
            pattern: None,
            spawn_rate: 1.0,
            spawn_count: 1,
        }
    }
    
    pub fn pattern(mut self, pattern: BulletPattern) -> Self {
        self.pattern = Some(pattern);
        self
    }
    
    pub fn spawn_rate(mut self, rate: f32) -> Self {
        self.spawn_rate = rate;
        self
    }
    
    pub fn build(self) -> Result<BulletEmitter, &'static str> {
        Ok(BulletEmitter {
            pattern: self.pattern.ok_or("必须指定弹幕模式")?,
            spawn_rate: self.spawn_rate,
            spawn_count: self.spawn_count,
            inherit_velocity: false,
        })
    }
}
```

## 下一步

- 阅读 [测试指南](testing-guide.md) 了解测试策略
- 阅读 [贡献流程](contributing.md) 了解如何贡献代码
