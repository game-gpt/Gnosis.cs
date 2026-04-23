namespace Gnosis.IR.Transform;

public sealed class OptimizationPipeline
{
    #region Properties

    public IrModule Module { get; }

    public int OptimizationLevel { get; }

    public OptimizationStatistics Statistics { get; private set; }

    #endregion

    #region Fields

    private readonly List<IOptimizationPass> _passes = [];

    #endregion

    #region Constructors

    public OptimizationPipeline(IrModule module, int optimizationLevel = 2)
    {
        Module = module;
        OptimizationLevel = optimizationLevel;
        Statistics = new OptimizationStatistics();
        ConfigurePasses();
    }

    #endregion

    #region Public Methods

    public bool Run()
    {
        Statistics = new OptimizationStatistics();
        bool anyChanged = false;
        int iteration = 0;
        const int maxIterations = 10;

        do
        {
            bool changed = false;
            iteration++;

            foreach (var pass in _passes)
            {
                if (pass.Changed)
                {
                    pass.Reset();
                }

                if (pass.Run())
                {
                    changed = true;
                    Statistics.RecordPass(pass.Name);
                }
            }

            if (changed)
            {
                anyChanged = true;
            }
            else
            {
                break;
            }
        } while (iteration < maxIterations);

        Statistics.TotalIterations = iteration;
        return anyChanged;
    }

    public void AddPass(IOptimizationPass pass)
    {
        _passes.Add(pass);
    }

    public void RemovePass(string passName)
    {
        _passes.RemoveAll(p => p.Name == passName);
    }

    #endregion

    #region Private Methods

    private void ConfigurePasses()
    {
        switch (OptimizationLevel)
        {
            case 0:
                break;

            case 1:
                _passes.Add(new ConstantFolder(Module));
                _passes.Add(new DeadCodeEliminator(Module));
                break;

            case 2:
                _passes.Add(new ConstantFolder(Module));
                _passes.Add(new DeadCodeEliminator(Module));
                _passes.Add(new Inliner(Module));
                _passes.Add(new DeadCodeEliminator(Module));
                break;

            case 3:
                _passes.Add(new ConstantFolder(Module));
                _passes.Add(new DeadCodeEliminator(Module));
                _passes.Add(new Inliner(Module));
                _passes.Add(new ConstantFolder(Module));
                _passes.Add(new DeadCodeEliminator(Module));
                _passes.Add(new LoopUnroller(Module));
                _passes.Add(new ConstantFolder(Module));
                _passes.Add(new DeadCodeEliminator(Module));
                break;
        }
    }

    #endregion
}

public interface IOptimizationPass
{
    string Name { get; }

    bool Changed { get; }

    bool Run();

    void Reset();
}

public sealed class OptimizationStatistics
{
    public int TotalIterations { get; set; }

    public IReadOnlyDictionary<string, int> PassExecutionCounts => _passExecutionCounts;

    private readonly Dictionary<string, int> _passExecutionCounts = [];

    public void RecordPass(string passName)
    {
        if (!_passExecutionCounts.ContainsKey(passName))
        {
            _passExecutionCounts[passName] = 0;
        }
        _passExecutionCounts[passName]++;
    }

    public override string ToString()
    {
        var details = string.Join(", ", _passExecutionCounts.Select(kv => $"{kv.Key}: {kv.Value}"));
        return $"迭代 {TotalIterations} 次, {details}";
    }
}
