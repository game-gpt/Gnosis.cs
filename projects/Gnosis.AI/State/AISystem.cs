using Gnosis.AI.Behavior;
using Gnosis.AI.Blackboard;
using Gnosis.Navigation.NavMesh;

namespace Gnosis.AI.State;

public sealed class AISystem : IAISystem
{
    #region 字段

    private readonly List<IAIController> _controllers = new();
    private INavigationSystem? _navigation;

    #endregion

    #region 属性

    public INavigationSystem Navigation => _navigation ?? throw new InvalidOperationException("导航系统未设置");

    #endregion

    #region 构造函数

    public AISystem()
    {
    }

    public AISystem(INavigationSystem navigation)
    {
        _navigation = navigation;
    }

    #endregion

    #region IAISystem 实现

    public IBehaviorTree CreateBehaviorTree(string name)
    {
        return new BehaviorTree(name);
    }

    public IAIController CreateController()
    {
        var controller = new AIController();
        _controllers.Add(controller);
        return controller;
    }

    public void DestroyController(IAIController controller)
    {
        _controllers.Remove(controller);
    }

    public void Update(float delta)
    {
        foreach (var controller in _controllers)
        {
            controller.Update(delta);
        }
    }

    #endregion

    #region 公开方法

    public void SetNavigation(INavigationSystem navigation)
    {
        _navigation = navigation;
    }

    #endregion
}
