# 测试策略与指南

本文档定义了 Gnosis 引擎项目的测试策略和指南。

## 测试层级

```
┌─────────────────────────────────────────────────────────────┐
│                     E2E 测试                                  │
│  完整的游戏流程测试，验证端到端功能                              │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                     集成测试                                  │
│  模块间交互测试，验证组件协作                                  │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                     单元测试                                  │
│  函数和类型测试，验证独立功能                                  │
└─────────────────────────────────────────────────────────────┘
```

## 单元测试

### 测试位置

单元测试放在源文件中，使用 `#[cfg(test)]` 模块：

```rust
// src/bullet_pattern.rs

pub fn calculate_bullet_position(angle: f32, speed: f32, time: f32) -> (f32, f32) {
    let x = speed * time * angle.cos();
    let y = speed * time * angle.sin();
    (x, y)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_calculate_bullet_position_zero_angle() {
        let (x, y) = calculate_bullet_position(0.0, 100.0, 1.0);
        assert!((x - 100.0).abs() < 0.001);
        assert!(y.abs() < 0.001);
    }

    #[test]
    fn test_calculate_bullet_position_90_degrees() {
        let (x, y) = calculate_bullet_position(std::f32::consts::FRAC_PI_2, 100.0, 1.0);
        assert!(x.abs() < 0.001);
        assert!((y - 100.0).abs() < 0.001);
    }
}
```

### 测试命名规范

测试函数命名应描述测试场景：

```rust
#[test]
fn test_<function>_<scenario>() {
    // ...
}

// 示例
#[test]
fn test_parse_script_empty_input() { }

#[test]
fn test_parse_script_invalid_syntax() { }

#[test]
fn test_bullet_emitter_creates_correct_count() { }
```

### 测试组织

```rust
#[cfg(test)]
mod tests {
    use super::*;

    mod bullet_pattern {
        use super::*;

        #[test]
        fn test_circle_pattern() { }

        #[test]
        fn test_spiral_pattern() { }
    }

    mod bullet_emitter {
        use super::*;

        #[test]
        fn test_spawn_rate() { }
    }
}
```

## 集成测试

### 测试位置

集成测试放在 `tests/` 目录下：

```
project/
├── src/
│   └── lib.rs
└── tests/
    ├── common/
    │   └── mod.rs      # 共享测试工具
    ├── integration_test.rs
    └── engine_test.rs
```

### 示例

```rust
// tests/engine_test.rs
use gg_engine_stg::engine::StgEngine;
use gg_engine_stg::config::StgConfig;

#[test]
fn test_engine_initialization() {
    let config = StgConfig::default();
    let engine = StgEngine::new(config, false);
    assert!(engine.initialize().is_ok());
}

#[test]
fn test_engine_run_and_shutdown() {
    let config = StgConfig::default();
    let mut engine = StgEngine::new(config, false);
    engine.initialize().unwrap();
    
    // 模拟运行几帧
    for _ in 0..10 {
        engine.update(0.016);
    }
    
    engine.shutdown();
}
```

### 共享测试工具

```rust
// tests/common/mod.rs
pub fn create_test_engine() -> StgEngine {
    let config = StgConfig::default();
    StgEngine::new(config, false)
}

pub fn create_test_player(world: &mut World) -> Entity {
    world.spawn_entity()
        .with(Transform::default())
        .with(Health { current: 100, max: 100 })
        .build()
}
```

## 性能测试

### Criterion 基准测试

```rust
// benches/bullet_bench.rs
use criterion::{black_box, criterion_group, criterion_main, Criterion};
use gg_engine_stg::bullet_pattern::BulletPattern;

fn bench_bullet_spawn(c: &mut Criterion) {
    let pattern = BulletPattern::Circle { count: 1000 };
    
    c.bench_function("spawn_1000_bullets", |b| {
        b.iter(|| {
            spawn_bullets(black_box(&pattern))
        })
    });
}

criterion_group!(benches, bench_bullet_spawn);
criterion_main!(benches);
```

### 运行基准测试

```bash
cargo bench
```

## 测试覆盖率

### 安装 tarpaulin

```bash
cargo install cargo-tarpaulin
```

### 生成覆盖率报告

```bash
cargo tarpaulin --out Html
```

### 覆盖率目标

| 模块 | 目标覆盖率 |
| :--- | :--- |
| 核心引擎 | 80% |
| 系统 | 70% |
| 组件 | 60% |
| 工具函数 | 90% |

## 模拟测试

### Mock 对象

```rust
pub trait NetworkBackend {
    fn send(&mut self, data: &[u8]) -> Result<(), Error>;
    fn receive(&mut self) -> Result<Vec<u8>, Error>;
}

pub struct MockNetworkBackend {
    pub sent_data: Vec<Vec<u8>>,
    pub receive_queue: VecDeque<Vec<u8>>,
}

impl NetworkBackend for MockNetworkBackend {
    fn send(&mut self, data: &[u8]) -> Result<(), Error> {
        self.sent_data.push(data.to_vec());
        Ok(())
    }
    
    fn receive(&mut self) -> Result<Vec<u8>, Error> {
        self.receive_queue.pop_front()
            .ok_or(Error::NoData)
    }
}

#[test]
fn test_network_manager_send() {
    let mut backend = MockNetworkBackend::new();
    let mut manager = NetworkManager::new(Box::new(backend));
    
    manager.send(b"test").unwrap();
    
    assert_eq!(manager.backend().sent_data.len(), 1);
    assert_eq!(manager.backend().sent_data[0], b"test");
}
```

## 测试夹具

### 使用 fixtures 目录

```
tests/
├── fixtures/
│   ├── scripts/
│   │   ├── valid.script
│   │   └── invalid.script
│   └── configs/
│       └── test_game.toml
└── integration_test.rs
```

### 加载测试资源

```rust
#[test]
fn test_parse_valid_script() {
    let content = include_str!("fixtures/scripts/valid.script");
    let result = parse_script(content);
    assert!(result.is_ok());
}
```

## CI 测试

### GitHub Actions

```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions-rs/toolchain@v1
        with:
          toolchain: stable
      - run: cargo test --all
      - run: cargo clippy -- -D warnings
```

## 测试最佳实践

### 避免测试私有函数

```rust
// 不推荐：测试私有函数
#[test]
fn test_private_helper() {
    assert_eq!(private_helper(1), 2);
}

// 推荐：通过公共接口测试
#[test]
fn test_public_function() {
    assert_eq!(public_function(1), 2);
}
```

### 使用有意义的断言

```rust
// 不推荐
assert!(result.is_ok());

// 推荐
assert!(result.is_ok(), "解析脚本失败: {:?}", result.err());
```

### 测试边界条件

```rust
#[test]
fn test_boundary_zero() {
    let result = calculate(0);
    assert_eq!(result, 0);
}

#[test]
fn test_boundary_max() {
    let result = calculate(u32::MAX);
    assert_eq!(result, expected_max);
}

#[test]
fn test_boundary_negative() {
    let result = calculate(-1);
    assert!(result.is_err());
}
```

### 测试错误路径

```rust
#[test]
fn test_invalid_input() {
    let result = parse_script("{{{ invalid");
    assert!(result.is_err());
    
    match result {
        Err(ParseError::SyntaxError { line, .. }) => {
            assert_eq!(line, 1);
        }
        _ => panic!("期望语法错误"),
    }
}
```

## 运行测试

### 运行所有测试

```bash
cargo test
```

### 运行特定测试

```bash
cargo test test_bullet_pattern
```

### 运行特定项目的测试

```bash
cargo test -p gg-engine-stg
```

### 显示测试输出

```bash
cargo test -- --nocapture
```

## 下一步

- 阅读 [编码规范](coding-standards.md) 了解代码风格
- 阅读 [贡献流程](contributing.md) 了解如何贡献代码
