//! 预导入模块

pub use crate::compiler::{
    codegen::GalgameCodegen,
    compiler::GalgameCompiler,
    error::{GalgameError, GalgameResult},
    ir::{
        CharacterDefIr, ChoiceIr, CommandIr, DialogueDB, DialogueIr, FrontMatterIr, GalgameIr, StorySequence, VariableValueIr,
    },
    parser::GalgameParser,
    transformer::GalgameTransformer,
};
