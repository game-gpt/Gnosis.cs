#![warn(missing_docs)]

//! GG Galgame 对话脚本编译器模块
//! 提供 .script 剧本脚本的编译、增量编译和转换功能

pub mod codegen;
pub mod compiler;
pub mod error;
pub mod incremental;
pub mod ir;
pub mod parser;
pub mod prelude;
pub mod transformer;

pub use compiler::GalgameCompiler;
pub use error::{GalgameError, GalgameResult};
pub use ir::GalgameIr;
pub use parser::GalgameParser;
pub use codegen::GalgameCodegen;
pub use transformer::GalgameTransformer;
