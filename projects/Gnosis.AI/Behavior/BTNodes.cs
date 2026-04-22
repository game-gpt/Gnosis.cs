using Gnosis.AI.Blackboard;

namespace Gnosis.AI.Behavior;

/// <summary>
/// 行为树节点集合，包含所有内置节点类型的实现
/// </summary>
public static class BTNodes
{
    #region 序列节点

    /// <summary>
    /// 序列节点，依次执行子节点，全部成功则成功，任一失败则失败
    /// </summary>
    public sealed class Sequence : CompositeNode
    {
        private int _currentChildIndex;

        public Sequence(string name) : base(name) { }

        public override BTNodeType NodeType => BTNodeType.Sequence;

        public override void Reset()
        {
            base.Reset();
            _currentChildIndex = 0;
        }

        protected override BTNodeStatus OnExecute()
        {
            while (_currentChildIndex < Children.Count)
            {
                var childStatus = Children[_currentChildIndex].Execute();

                if (childStatus == BTNodeStatus.Running)
                {
                    return BTNodeStatus.Running;
                }

                if (childStatus == BTNodeStatus.Failure)
                {
                    _currentChildIndex = 0;
                    return BTNodeStatus.Failure;
                }

                _currentChildIndex++;
            }

            _currentChildIndex = 0;
            return BTNodeStatus.Success;
        }
    }

    #endregion

    #region 选择节点

    /// <summary>
    /// 选择节点，依次执行子节点，任一成功则成功，全部失败则失败
    /// </summary>
    public sealed class Selector : CompositeNode
    {
        private int _currentChildIndex;

        public Selector(string name) : base(name) { }

        public override BTNodeType NodeType => BTNodeType.Selector;

        public override void Reset()
        {
            base.Reset();
            _currentChildIndex = 0;
        }

        protected override BTNodeStatus OnExecute()
        {
            while (_currentChildIndex < Children.Count)
            {
                var childStatus = Children[_currentChildIndex].Execute();

                if (childStatus == BTNodeStatus.Running)
                {
                    return BTNodeStatus.Running;
                }

                if (childStatus == BTNodeStatus.Success)
                {
                    _currentChildIndex = 0;
                    return BTNodeStatus.Success;
                }

                _currentChildIndex++;
            }

            _currentChildIndex = 0;
            return BTNodeStatus.Failure;
        }
    }

    #endregion

    #region 并行节点

    /// <summary>
    /// 并行节点，同时执行所有子节点
    /// </summary>
    public sealed class Parallel : CompositeNode
    {
        private readonly int _requiredSuccessCount;
        private readonly int _requiredFailureCount;

        public Parallel(string name, int requiredSuccessCount, int requiredFailureCount) : base(name)
        {
            _requiredSuccessCount = requiredSuccessCount;
            _requiredFailureCount = requiredFailureCount;
        }

        public override BTNodeType NodeType => BTNodeType.Parallel;

        protected override BTNodeStatus OnExecute()
        {
            var successCount = 0;
            var failureCount = 0;

            foreach (var child in Children)
            {
                var childStatus = child.Execute();

                if (childStatus == BTNodeStatus.Success)
                {
                    successCount++;
                }
                else if (childStatus == BTNodeStatus.Failure)
                {
                    failureCount++;
                }
            }

            if (successCount >= _requiredSuccessCount)
            {
                return BTNodeStatus.Success;
            }

            if (failureCount >= _requiredFailureCount)
            {
                return BTNodeStatus.Failure;
            }

            return BTNodeStatus.Running;
        }
    }

    #endregion

    #region 装饰器节点

    /// <summary>
    /// 反转装饰器，将子节点结果取反
    /// </summary>
    public sealed class Inverter : DecoratorNode
    {
        public Inverter(string name) : base(name) { }

        public override BTNodeType NodeType => BTNodeType.Decorator;

        protected override BTNodeStatus OnExecute()
        {
            if (Child is null)
            {
                return BTNodeStatus.Failure;
            }

            var childStatus = Child.Execute();

            return childStatus switch
            {
                BTNodeStatus.Success => BTNodeStatus.Failure,
                BTNodeStatus.Failure => BTNodeStatus.Success,
                _ => BTNodeStatus.Running
            };
        }
    }

    /// <summary>
    /// 重复装饰器，重复执行子节点直到失败
    /// </summary>
    public sealed class Repeater : DecoratorNode
    {
        private readonly int _maxRepeats;
        private int _repeatCount;

        public Repeater(string name, int maxRepeats = -1) : base(name)
        {
            _maxRepeats = maxRepeats;
        }

        public override BTNodeType NodeType => BTNodeType.Decorator;

        public override void Reset()
        {
            base.Reset();
            _repeatCount = 0;
        }

        protected override BTNodeStatus OnExecute()
        {
            if (Child is null)
            {
                return BTNodeStatus.Failure;
            }

            var childStatus = Child.Execute();

            if (childStatus == BTNodeStatus.Failure)
            {
                return BTNodeStatus.Failure;
            }

            _repeatCount++;

            if (_maxRepeats > 0 && _repeatCount >= _maxRepeats)
            {
                return BTNodeStatus.Success;
            }

            if (childStatus == BTNodeStatus.Success)
            {
                Child.Reset();
            }

            return BTNodeStatus.Running;
        }
    }

    /// <summary>
    /// 成功装饰器，无论子节点结果如何都返回成功
    /// </summary>
    public sealed class Succeeder : DecoratorNode
    {
        public Succeeder(string name) : base(name) { }

        public override BTNodeType NodeType => BTNodeType.Decorator;

        protected override BTNodeStatus OnExecute()
        {
            if (Child is not null)
            {
                Child.Execute();
            }

            return BTNodeStatus.Success;
        }
    }

    #endregion

    #region 条件节点

    /// <summary>
    /// 条件节点，根据条件函数返回成功或失败
    /// </summary>
    public sealed class Condition : BTNode
    {
        private readonly Func<IBlackboard, bool> _condition;

        private readonly IBlackboard _blackboard;

        public override BTNodeType NodeType => BTNodeType.Condition;

        public Condition(string name, Func<IBlackboard, bool> condition, IBlackboard blackboard) : base(name)
        {
            _condition = condition;
            _blackboard = blackboard;
        }

        protected override BTNodeStatus OnExecute()
        {
            return _condition(_blackboard) ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }
    }

    #endregion

    #region 任务节点

    /// <summary>
    /// 任务节点，执行具体的行为逻辑
    /// </summary>
    public sealed class Task : BTNode
    {
        private readonly Func<IBlackboard, BTNodeStatus> _task;

        private readonly IBlackboard _blackboard;

        public override BTNodeType NodeType => BTNodeType.Task;

        public Task(string name, Func<IBlackboard, BTNodeStatus> task, IBlackboard blackboard) : base(name)
        {
            _task = task;
            _blackboard = blackboard;
        }

        protected override BTNodeStatus OnExecute()
        {
            return _task(_blackboard);
        }
    }

    #endregion
}

/// <summary>
/// 组合节点基类，包含多个子节点
/// </summary>
public abstract class CompositeNode : BTNode
{
    #region 字段

    private readonly List<IBTNode> _children = new();

    #endregion

    #region 属性

    /// <summary>
    /// 子节点列表
    /// </summary>
    public IReadOnlyList<IBTNode> Children => _children;

    #endregion

    #region 构造函数

    protected CompositeNode(string name) : base(name) { }

    #endregion

    #region 公开方法

    /// <summary>
    /// 添加子节点
    /// </summary>
    public void AddChild(IBTNode child)
    {
        _children.Add(child);
    }

    /// <summary>
    /// 移除子节点
    /// </summary>
    public void RemoveChild(IBTNode child)
    {
        _children.Remove(child);
    }

    #endregion

    #region 重写方法

    public override void Reset()
    {
        base.Reset();
        foreach (var child in _children)
        {
            child.Reset();
        }
    }

    #endregion
}

/// <summary>
/// 装饰器节点基类，包含单个子节点
/// </summary>
public abstract class DecoratorNode : BTNode
{
    #region 属性

    /// <summary>
    /// 子节点
    /// </summary>
    public IBTNode? Child { get; private set; }

    #endregion

    #region 构造函数

    protected DecoratorNode(string name) : base(name) { }

    #endregion

    #region 公开方法

    /// <summary>
    /// 设置子节点
    /// </summary>
    public void SetChild(IBTNode child)
    {
        Child = child;
    }

    #endregion

    #region 重写方法

    public override void Reset()
    {
        base.Reset();
        Child?.Reset();
    }

    #endregion
}
