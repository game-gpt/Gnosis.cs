namespace Gnosis.Graphic.Shader.Spirv;

public static class SpirvConstants
{
    public const uint MagicNumber = 0x07230203;
    public const uint Version10 = 0x00010000;
    public const uint Version11 = 0x00010100;
    public const uint Version12 = 0x00010200;
    public const uint Version13 = 0x00010300;
    public const uint Version14 = 0x00010400;
    public const uint Version15 = 0x00010500;
    public const uint GeneratorMagicNumber = 0x00470000;
    public const uint Schema = 0;

    public static class Capability
    {
        public const uint Matrix = 0;
        public const uint Shader = 1;
        public const uint Geometry = 2;
        public const uint Tessellation = 3;
        public const uint Addresses = 4;
        public const uint Linkage = 5;
        public const uint Kernel = 6;
        public const uint Vector16 = 7;
        public const uint Float16Buffer = 8;
        public const uint Float16 = 9;
        public const uint Float64 = 10;
        public const uint Int64 = 11;
        public const uint Int64Atomics = 12;
        public const uint ImageBasic = 13;
        public const uint ImageReadWrite = 14;
        public const uint ImageMipmap = 15;
        public const uint Pipes = 17;
        public const uint Groups = 18;
        public const uint DeviceEnqueue = 19;
        public const uint LiteralSampler = 20;
        public const uint AtomicStorage = 21;
        public const uint Int16 = 22;
        public const uint TessellationPointSize = 23;
        public const uint GeometryPointSize = 24;
        public const uint ImageGatherExtended = 25;
        public const uint StorageImageMultisample = 27;
        public const uint UniformBufferArrayDynamicIndexing = 28;
        public const uint SampledImageArrayDynamicIndexing = 29;
        public const uint StorageBufferArrayDynamicIndexing = 30;
        public const uint StorageImageArrayDynamicIndexing = 31;
        public const uint ClipDistance = 32;
        public const uint CullDistance = 33;
        public const uint ImageCubeArray = 34;
        public const uint SampleRateShading = 35;
        public const uint ImageRect = 36;
        public const uint SampledRect = 37;
        public const uint GenericPointer = 38;
        public const uint Int8 = 39;
        public const uint InputAttachment = 40;
        public const uint SparseResidency = 41;
        public const uint MinLod = 42;
        public const uint Sampled1D = 43;
        public const uint Image1D = 44;
        public const uint SampledCubeArray = 45;
        public const uint SampledBuffer = 46;
        public const uint ImageBuffer = 47;
        public const uint ImageMSArray = 48;
        public const uint StorageImageExtendedFormats = 49;
        public const uint ImageQuery = 50;
        public const uint DerivativeControl = 51;
        public const uint InterpolationFunction = 52;
        public const uint TransformFeedback = 53;
        public const uint GeometryStreams = 54;
        public const uint StorageImageReadWithoutFormat = 55;
        public const uint StorageImageWriteWithoutFormat = 56;
        public const uint MultiViewport = 57;
        public const uint SubgroupDispatch = 58;
        public const uint NamedBarrier = 59;
        public const uint MeshShadingNV = 60;
        public const uint RayTracingKHR = 4479;
        public const uint RayQueryKHR = 4472;
        public const uint VulkanMemoryModel = 4434;
    }

    public static class Op
    {
        public const uint OpNop = 0;
        public const uint OpUndef = 1;
        public const uint OpSource = 3;
        public const uint OpName = 5;
        public const uint OpMemberName = 6;
        public const uint OpExtInstImport = 11;
        public const uint OpExtInst = 12;
        public const uint OpMemoryModel = 14;
        public const uint OpEntryPoint = 15;
        public const uint OpExecutionMode = 16;
        public const uint OpCapability = 17;
        public const uint OpTypeVoid = 19;
        public const uint OpTypeBool = 20;
        public const uint OpTypeInt = 21;
        public const uint OpTypeFloat = 22;
        public const uint OpTypeVector = 23;
        public const uint OpTypeMatrix = 24;
        public const uint OpTypeImage = 25;
        public const uint OpTypeSampler = 26;
        public const uint OpTypeSampledImage = 27;
        public const uint OpTypeArray = 28;
        public const uint OpTypeRuntimeArray = 29;
        public const uint OpTypeStruct = 30;
        public const uint OpTypePointer = 32;
        public const uint OpTypeFunction = 33;
        public const uint OpTypeAccelerationStructureKHR = 5343;
        public const uint OpConstantTrue = 41;
        public const uint OpConstantFalse = 42;
        public const uint OpConstant = 43;
        public const uint OpSpecConstant = 44;
        public const uint OpFunction = 54;
        public const uint OpFunctionParameter = 55;
        public const uint OpFunctionEnd = 56;
        public const uint OpFunctionCall = 57;
        public const uint OpVariable = 59;
        public const uint OpLoad = 61;
        public const uint OpStore = 62;
        public const uint OpCopyMemory = 63;
        public const uint OpAccessChain = 65;
        public const uint OpDecorate = 71;
        public const uint OpMemberDecorate = 72;
        public const uint OpLabel = 248;
        public const uint OpBranch = 249;
        public const uint OpBranchConditional = 250;
        public const uint OpReturn = 253;
        public const uint OpReturnValue = 254;
        public const uint OpKill = 252;
        public const uint OpSelectionMerge = 247;
        public const uint OpLoopMerge = 246;
        public const uint OpPhi = 195;
        public const uint OpCompositeConstruct = 80;
        public const uint OpCompositeExtract = 81;
        public const uint OpVectorShuffle = 79;
        public const uint OpConvertFToU = 109;
        public const uint OpConvertFToS = 110;
        public const uint OpConvertUToF = 111;
        public const uint OpConvertSToF = 112;
        public const uint OpIAdd = 128;
        public const uint OpISub = 129;
        public const uint OpIMul = 130;
        public const uint OpSDiv = 131;
        public const uint OpUDiv = 132;
        public const uint OpFAdd = 136;
        public const uint OpFSub = 137;
        public const uint OpFMul = 138;
        public const uint OpFDiv = 139;
        public const uint OpFNegate = 127;
        public const uint OpSNegate = 126;
        public const uint OpIEqual = 162;
        public const uint OpINotEqual = 163;
        public const uint OpSLessThan = 170;
        public const uint OpSGreaterThan = 168;
        public const uint OpSLessEqual = 171;
        public const uint OpSGreaterEqual = 169;
        public const uint OpULessThan = 166;
        public const uint OpFOrdEqual = 172;
        public const uint OpFOrdNotEqual = 173;
        public const uint OpFOrdLessThan = 174;
        public const uint OpFOrdGreaterThan = 175;
        public const uint OpFOrdLessEqual = 176;
        public const uint OpFOrdGreaterEqual = 177;
        public const uint OpLogicalAnd = 180;
        public const uint OpLogicalOr = 181;
        public const uint OpLogicalNot = 182;
        public const uint OpDot = 148;
        public const uint OpMatrixTimesVector = 145;
        public const uint OpSampledImage = 86;
        public const uint OpImageSampleImplicitLod = 87;
        public const uint OpImageFetch = 95;
        public const uint OpImageWrite = 99;
        public const uint OpExtension = 10;
        public const uint OpSpecConstantOp = 52;
        public const uint OpRayQueryInitializeKHR = 5345;
        public const uint OpRayQueryProceedKHR = 5346;
        public const uint OpRayQueryGetIntersectionTypeKHR = 5349;
    }

    public static class ExecutionModel
    {
        public const uint Vertex = 0;
        public const uint TessellationControl = 1;
        public const uint TessellationEvaluation = 2;
        public const uint Geometry = 3;
        public const uint Fragment = 4;
        public const uint GLCompute = 5;
        public const uint RayGenerationKHR = 5313;
        public const uint IntersectionKHR = 5314;
        public const uint AnyHitKHR = 5315;
        public const uint ClosestHitKHR = 5316;
        public const uint MissKHR = 5317;
    }

    public static class ExecutionMode
    {
        public const uint Invocations = 0;
        public const uint SpacingEqual = 1;
        public const uint SpacingFractionalEven = 2;
        public const uint SpacingFractionalOdd = 3;
        public const uint VertexOrderCw = 4;
        public const uint VertexOrderCcw = 5;
        public const uint PixelCenterInteger = 6;
        public const uint OriginUpperLeft = 7;
        public const uint OriginLowerLeft = 8;
        public const uint EarlyFragmentTests = 9;
        public const uint PointMode = 10;
        public const uint Xfb = 11;
        public const uint DepthReplacing = 12;
        public const uint DepthGreater = 14;
        public const uint DepthLess = 15;
        public const uint DepthUnchanged = 16;
        public const uint LocalSize = 17;
        public const uint LocalSizeHint = 18;
        public const uint InputPoints = 19;
        public const uint InputLines = 20;
        public const uint InputLinesAdjacency = 21;
        public const uint Triangles = 22;
        public const uint InputTrianglesAdjacency = 23;
        public const uint Quads = 24;
        public const uint Isolines = 25;
        public const uint OutputVertices = 26;
        public const uint OutputPoints = 27;
        public const uint OutputLineStrip = 28;
        public const uint OutputTriangleStrip = 29;
    }

    public static class StorageClass
    {
        public const uint UniformConstant = 0;
        public const uint Input = 1;
        public const uint Uniform = 2;
        public const uint Output = 3;
        public const uint Workgroup = 4;
        public const uint CrossWorkgroup = 5;
        public const uint Private = 6;
        public const uint Function = 7;
        public const uint Generic = 8;
        public const uint PushConstant = 9;
        public const uint AtomicCounter = 10;
        public const uint Image = 11;
        public const uint StorageBuffer = 12;
        public const uint RayPayloadKHR = 33;
        public const uint HitAttributeKHR = 34;
        public const uint IncomingRayPayloadKHR = 35;
        public const uint ShaderRecordBufferKHR = 36;
    }

    public static class Decoration
    {
        public const uint Block = 2;
        public const uint BufferBlock = 3;
        public const uint RowMajor = 4;
        public const uint ColMajor = 5;
        public const uint ArrayStride = 6;
        public const uint MatrixStride = 7;
        public const uint GLSLShared = 8;
        public const uint GLSLPacked = 9;
        public const uint CPacked = 10;
        public const uint BuiltIn = 11;
        public const uint NoPerspective = 13;
        public const uint Flat = 14;
        public const uint Patch = 15;
        public const uint Centroid = 16;
        public const uint Sample = 17;
        public const uint Invariant = 18;
        public const uint Restrict = 19;
        public const uint Aliased = 20;
        public const uint Volatile = 21;
        public const uint Constant = 22;
        public const uint Coherent = 23;
        public const uint NonWritable = 24;
        public const uint NonReadable = 25;
        public const uint Uniform = 26;
        public const uint UniformId = 27;
        public const uint SaturatedConversion = 28;
        public const uint Stream = 29;
        public const uint Location = 30;
        public const uint Component = 31;
        public const uint Index = 32;
        public const uint Binding = 33;
        public const uint DescriptorSet = 34;
        public const uint Offset = 35;
        public const uint SpecId = 41;
    }

    public static class BuiltIn
    {
        public const uint Position = 0;
        public const uint PointSize = 1;
        public const uint ClipDistance = 3;
        public const uint CullDistance = 4;
        public const uint VertexId = 5;
        public const uint InstanceId = 6;
        public const uint PrimitiveId = 7;
        public const uint InvocationId = 8;
        public const uint Layer = 9;
        public const uint ViewportIndex = 10;
        public const uint TessLevelOuter = 11;
        public const uint TessLevelInner = 12;
        public const uint TessCoord = 13;
        public const uint PatchVertices = 14;
        public const uint FragCoord = 15;
        public const uint PointCoord = 16;
        public const uint FrontFacing = 17;
        public const uint SampleId = 18;
        public const uint SamplePosition = 19;
        public const uint SampleMask = 20;
        public const uint FragDepth = 22;
        public const uint HelperInvocation = 23;
        public const uint NumWorkgroups = 24;
        public const uint WorkgroupSize = 25;
        public const uint WorkgroupId = 26;
        public const uint LocalInvocationId = 27;
        public const uint GlobalInvocationId = 28;
        public const uint LocalInvocationIndex = 29;
        public const uint WorkDim = 30;
        public const uint GlobalSize = 31;
        public const uint EnqueuedWorkgroupSize = 32;
        public const uint GlobalOffset = 33;
        public const uint GlobalLinearId = 34;
        public const uint SubgroupSize = 36;
        public const uint SubgroupMaxSize = 37;
        public const uint NumSubgroups = 38;
        public const uint NumEnqueuedSubgroups = 39;
        public const uint SubgroupId = 40;
        public const uint SubgroupLocalInvocationId = 41;
        public const uint VertexIndex = 42;
        public const uint InstanceIndex = 43;
        public const uint LaunchIdKHR = 5319;
        public const uint LaunchSizeKHR = 5320;
        public const uint WorldRayOriginKHR = 5321;
        public const uint WorldRayDirectionKHR = 5322;
        public const uint ObjectRayOriginKHR = 5323;
        public const uint ObjectRayDirectionKHR = 5324;
        public const uint RayTminKHR = 5325;
        public const uint RayTmaxKHR = 5326;
        public const uint InstanceCustomIndexKHR = 5327;
        public const uint ObjectToWorldKHR = 5330;
        public const uint WorldToObjectKHR = 5331;
        public const uint HitTKHR = 5332;
        public const uint HitKindKHR = 5333;
    }

    public static class AddressingModel
    {
        public const uint Logical = 0;
        public const uint Physical32 = 1;
        public const uint Physical64 = 2;
    }

    public static class MemoryModel
    {
        public const uint Simple = 0;
        public const uint GLSL450 = 1;
        public const uint OpenCL = 2;
        public const uint Vulkan = 3;
    }

    public static class ImageDim
    {
        public const uint Dim1D = 0;
        public const uint Dim2D = 1;
        public const uint Dim3D = 2;
        public const uint DimCube = 3;
        public const uint DimRect = 4;
        public const uint DimBuffer = 5;
        public const uint DimSubpassData = 6;
    }

    public static class GLSLstd450
    {
        public const uint Round = 1;
        public const uint RoundEven = 2;
        public const uint Trunc = 3;
        public const uint FAbs = 4;
        public const uint SAbs = 5;
        public const uint FSign = 6;
        public const uint SSign = 7;
        public const uint Floor = 8;
        public const uint Ceil = 9;
        public const uint Fract = 10;
        public const uint Radians = 11;
        public const uint Degrees = 12;
        public const uint Sin = 13;
        public const uint Cos = 14;
        public const uint Tan = 15;
        public const uint Asin = 16;
        public const uint Acos = 17;
        public const uint Atan = 18;
        public const uint Sinh = 19;
        public const uint Cosh = 20;
        public const uint Tanh = 21;
        public const uint Asinh = 22;
        public const uint Acosh = 23;
        public const uint Atanh = 24;
        public const uint Atan2 = 25;
        public const uint Pow = 26;
        public const uint Exp = 27;
        public const uint Log = 28;
        public const uint Exp2 = 29;
        public const uint Log2 = 30;
        public const uint Sqrt = 31;
        public const uint InverseSqrt = 32;
        public const uint Determinant = 33;
        public const uint MatrixInverse = 34;
        public const uint Modf = 35;
        public const uint ModfStruct = 36;
        public const uint FMin = 37;
        public const uint UMin = 38;
        public const uint SMin = 39;
        public const uint FMax = 40;
        public const uint UMax = 41;
        public const uint SMax = 42;
        public const uint FClamp = 43;
        public const uint UClamp = 44;
        public const uint SClamp = 45;
        public const uint FMix = 46;
        public const uint IMix = 47;
        public const uint Step = 48;
        public const uint SmoothStep = 49;
        public const uint Fma = 50;
        public const uint Frexp = 52;
        public const uint Ldexp = 53;
        public const uint PackSnorm4x8 = 54;
        public const uint PackUnorm4x8 = 55;
        public const uint PackSnorm2x16 = 56;
        public const uint PackUnorm2x16 = 57;
        public const uint PackHalf2x16 = 58;
        public const uint PackDouble2x32 = 59;
        public const uint UnpackSnorm2x16 = 60;
        public const uint UnpackUnorm2x16 = 61;
        public const uint UnpackHalf2x16 = 62;
        public const uint UnpackSnorm4x8 = 63;
        public const uint UnpackUnorm4x8 = 64;
        public const uint UnpackDouble2x32 = 65;
        public const uint Length = 66;
        public const uint Distance = 67;
        public const uint Cross = 68;
        public const uint Normalize = 69;
        public const uint FaceForward = 70;
        public const uint Reflect = 71;
        public const uint Refract = 72;
        public const uint FindILsb = 73;
        public const uint FindSMsb = 74;
        public const uint FindUMsb = 75;
        public const uint InterpolateAtCentroid = 76;
        public const uint InterpolateAtSample = 77;
        public const uint InterpolateAtOffset = 78;
        public const uint NMin = 79;
        public const uint NMax = 80;
        public const uint NClamp = 81;
    }

    public static class SourceLanguage
    {
        public const uint Unknown = 0;
        public const uint ESSL = 1;
        public const uint GLSL = 2;
        public const uint OpenCL_C = 3;
        public const uint OpenCL_CPP = 4;
        public const uint HLSL = 5;
    }
}
