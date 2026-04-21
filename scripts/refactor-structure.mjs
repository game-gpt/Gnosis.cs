import { existsSync, mkdirSync, renameSync, rmSync, writeFileSync } from 'fs';
import { dirname, join, relative } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const projectRoot = join(__dirname, '..');
const gnosisRoot = join(projectRoot, 'projects', 'Gnosis');

const logFile = join(__dirname, 'refactor-moves.log');
const logLines = [];

function log(message) {
    console.log(message);
    logLines.push(`[${new Date().toISOString()}] ${message}`);
}

function ensureDir(dir) {
    if (!existsSync(dir)) {
        mkdirSync(dir, { recursive: true });
        log(`[创建目录] ${relative(projectRoot, dir)}`);
    }
}

function moveFile(src, dest) {
    const srcPath = join(gnosisRoot, src);
    const destPath = join(gnosisRoot, dest);
    
    if (!existsSync(srcPath)) {
        log(`[跳过] 源文件不存在: ${src}`);
        return false;
    }
    
    ensureDir(dirname(destPath));
    renameSync(srcPath, destPath);
    log(`[移动] ${src} → ${dest}`);
    return true;
}

function moveDir(src, dest) {
    const srcPath = join(gnosisRoot, src);
    const destPath = join(gnosisRoot, dest);
    
    if (!existsSync(srcPath)) {
        log(`[跳过] 源目录不存在: ${src}`);
        return false;
    }
    
    ensureDir(dirname(destPath));
    renameSync(srcPath, destPath);
    log(`[移动目录] ${src} → ${dest}`);
    return true;
}

function removeDirIfEmpty(dir) {
    const dirPath = join(gnosisRoot, dir);
    if (existsSync(dirPath)) {
        try {
            rmSync(dirPath, { recursive: true });
            log(`[删除空目录] ${dir}`);
        } catch (e) {
            // 目录非空，忽略
        }
    }
}

log('========================================');
log('Gnosis 项目结构重构 - 文件移动脚本');
log('========================================\n');

// ========================================
// Phase 1: Core/ 目录拆分
// ========================================
log('\n--- Phase 1: Core/ 目录拆分 ---\n');

// 1.1 Events/ 移动到 Infrastructure/Events/
log('[1.1] 移动 Core/Events/ → Infrastructure/Events/');
moveDir('Core/Events', 'Infrastructure/Events');

// 1.2 ECS 相关文件移动到 ECS/Core/
log('\n[1.2] 移动 ECS 核心类型到 ECS/Core/');
ensureDir(join(gnosisRoot, 'ECS', 'Core'));
moveFile('Core/EntityId.cs', 'ECS/Core/EntityId.cs');
moveFile('Core/IComponent.cs', 'ECS/Core/IComponent.cs');
moveFile('Core/IEntity.cs', 'ECS/Core/IEntity.cs');
moveFile('Core/ISystem.cs', 'ECS/Core/ISystem.cs');
moveFile('Core/PlayerId.cs', 'ECS/Core/PlayerId.cs');
moveFile('Core/Position.cs', 'ECS/Core/Position.cs');
moveFile('Core/RegionId.cs', 'ECS/Core/RegionId.cs');

// 1.3 基础设施相关文件移动到 Infrastructure/
log('\n[1.3] 移动基础设施文件到 Infrastructure/');
moveFile('Core/BuildStage.cs', 'Infrastructure/BuildStage.cs');
moveFile('Core/ConfigSystem.cs', 'Infrastructure/ConfigSystem.cs');
moveFile('Core/PlatformType.cs', 'Infrastructure/PlatformType.cs');
moveFile('Core/TimeManager.cs', 'Infrastructure/TimeManager.cs');
moveFile('Core/Timestamp.cs', 'Infrastructure/Timestamp.cs');

// 1.4 删除空的 Core/ 目录
log('\n[1.4] 清理 Core/ 目录');
removeDirIfEmpty('Core');

// ========================================
// Phase 2: WidgetCompiler 移入 Compiler
// ========================================
log('\n--- Phase 2: WidgetCompiler 移入 Compiler ---\n');

moveFile('Editor/WidgetCompiler/GgWidgetCompiler.cs', 'Compiler/Frontend/GgWidgetCompiler.cs');
removeDirIfEmpty('Editor/WidgetCompiler');

// ========================================
// Phase 3: ECS 目录重组
// ========================================
log('\n--- Phase 3: ECS 目录重组 ---\n');

// 3.1 创建 Interface/ 目录并移动接口文件
log('[3.1] 创建 ECS/Interface/ 并移动接口文件');
ensureDir(join(gnosisRoot, 'ECS', 'Interface'));
moveFile('ECS/IArchetype.cs', 'ECS/Interface/IArchetype.cs');
moveFile('ECS/IComponentPool.cs', 'ECS/Interface/IComponentPool.cs');
moveFile('ECS/IComponentStorage.cs', 'ECS/Interface/IComponentStorage.cs');
moveFile('ECS/IQuery.cs', 'ECS/Interface/IQuery.cs');
moveFile('ECS/ISystemGroup.cs', 'ECS/Interface/ISystemGroup.cs');
moveFile('ECS/ISystemScheduler.cs', 'ECS/Interface/ISystemScheduler.cs');
moveFile('ECS/IWorld.cs', 'ECS/Interface/IWorld.cs');

// 3.2 创建 Implementation/ 目录
log('\n[3.2] 创建 ECS/Implementation/ 目录');
ensureDir(join(gnosisRoot, 'ECS', 'Implementation'));

// 3.3 移动现有实现文件到 Implementation/
log('\n[3.3] 移动实现文件到 ECS/Implementation/');
moveFile('ECS/ComponentPool.cs', 'ECS/Implementation/ComponentPool.cs');
moveFile('ECS/ComponentStorage.cs', 'ECS/Implementation/ComponentStorage.cs');

// ========================================
// Phase 4: Network 目录重组
// ========================================
log('\n--- Phase 4: Network 目录重组 ---\n');

// 4.1 创建目录结构
log('[4.1] 创建 Network 子目录');
ensureDir(join(gnosisRoot, 'Network', 'Core'));
ensureDir(join(gnosisRoot, 'Network', 'Backends'));
ensureDir(join(gnosisRoot, 'Network', 'Messages'));
ensureDir(join(gnosisRoot, 'Network', 'Sync'));

// 4.2 移动核心接口到 Core/
log('\n[4.2] 移动核心接口到 Network/Core/');
moveFile('Network/INetworkBackend.cs', 'Network/Core/INetworkBackend.cs');
moveFile('Network/INetworkManager.cs', 'Network/Core/INetworkManager.cs');
moveFile('Network/IMessageSerializer.cs', 'Network/Core/IMessageSerializer.cs');
moveFile('Network/NetworkBackendBase.cs', 'Network/Core/NetworkBackendBase.cs');

// 4.3 移动后端实现到 Backends/
log('\n[4.3] 移动后端实现到 Network/Backends/');
moveFile('Network/SteamNetworkBackend.cs', 'Network/Backends/SteamNetworkBackend.cs');
moveFile('Network/NullNetworkBackend.cs', 'Network/Backends/NullNetworkBackend.cs');

// 4.4 移动消息相关到 Messages/
log('\n[4.4] 移动消息定义到 Network/Messages/');
moveFile('Network/NetworkMessage.cs', 'Network/Messages/NetworkMessage.cs');
moveFile('Network/MessageType.cs', 'Network/Messages/MessageType.cs');

// 4.5 移动同步模式到 Sync/
log('\n[4.5] 移动同步模式到 Network/Sync/');
moveFile('Network/ILockstepSystem.cs', 'Network/Sync/ILockstepSystem.cs');
moveFile('Network/IStateSyncSystem.cs', 'Network/Sync/IStateSyncSystem.cs');
moveFile('Network/SyncMode.cs', 'Network/Sync/SyncMode.cs');

// 4.6 移动其他文件
log('\n[4.6] 移动其他网络文件');
moveFile('Network/ConnectionState.cs', 'Network/Core/ConnectionState.cs');
moveFile('Network/NetworkManager.cs', 'Network/Core/NetworkManager.cs');
moveFile('Network/NetworkMode.cs', 'Network/Core/NetworkMode.cs');
moveFile('Network/NetworkBackendType.cs', 'Network/Core/NetworkBackendType.cs');
moveFile('Network/ReplicatedAttribute.cs', 'Network/Core/ReplicatedAttribute.cs');

// ========================================
// Phase 5: Assets 目录补充
// ========================================
log('\n--- Phase 5: Assets 目录补充 ---\n');

log('[5.1] 创建 Assets/Pipeline/ 目录');
ensureDir(join(gnosisRoot, 'Assets', 'Pipeline'));

log('[5.2] 创建 Assets/VFS/ 目录');
ensureDir(join(gnosisRoot, 'Assets', 'VFS'));

// 移动 IVirtualFileSystem 到 VFS/
moveFile('Infrastructure/IVirtualFileSystem.cs', 'Assets/VFS/IVirtualFileSystem.cs');

// ========================================
// Phase 6: Rendering 目录补充
// ========================================
log('\n--- Phase 6: Rendering 目录补充 ---\n');

log('[6.1] 创建 Rendering/Backends/ 目录结构');
ensureDir(join(gnosisRoot, 'Rendering', 'Backends', 'Vulkan'));
ensureDir(join(gnosisRoot, 'Rendering', 'Backends', 'Metal'));
ensureDir(join(gnosisRoot, 'Rendering', 'Backends', 'D3D12'));
ensureDir(join(gnosisRoot, 'Rendering', 'Backends', 'Software'));

// 移动软件渲染器
log('[6.2] 移动软件渲染器到 Backends/Software/');
moveFile('Rendering/Renderer/SoftwareRenderer.cs', 'Rendering/Backends/Software/SoftwareRenderer.cs');
moveFile('Rendering/Renderer/Texture.cs', 'Rendering/Backends/Software/Texture.cs');

// 清理空的 Renderer/ 目录
removeDirIfEmpty('Rendering/Renderer');

// ========================================
// 完成
// ========================================
log('\n========================================');
log('重构完成！');
log('========================================\n');

log('注意事项：');
log('1. 文件内容未修改，namespace 需要后续手动更新');
log('2. 部分空目录已删除');
log('3. 新增目录已创建占位符');
log('4. 请检查 .csproj 文件中的引用路径');

// 写入日志
writeFileSync(logFile, logLines.join('\n'), 'utf-8');
log(`\n日志已保存到: ${logFile}`);
