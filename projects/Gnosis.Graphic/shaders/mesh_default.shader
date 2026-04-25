struct MeshVertexInput {
    position: vec3<f32>,
    color: vec3<f32>,
}

struct MeshVertexOutput {
    position: vec4<f32>,
    color: vec3<f32>,
}

[Vertex]
micro vs_main(input: MeshVertexInput) -> MeshVertexOutput {
    let mut output: MeshVertexOutput;
    output.position = vec4<f32>(input.position, 1.0);
    output.color = input.color;
    return output;
}

[Fragment]
micro fs_main(input: MeshVertexOutput) -> vec4<f32> {
    return vec4<f32>(input.color, 1.0);
}
