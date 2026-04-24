struct SkyboxVertexInput {
    position: vec3<f32>,
    normal: vec3<f32>,
    uv: vec2<f32>,
}

struct SkyboxVertexOutput {
    position: vec4<f32>,
    uv: vec2<f32>,
    ray_direction: vec3<f32>,
}

[Vertex]
micro vs_main(input: SkyboxVertexInput) -> SkyboxVertexOutput {
    let mut output: SkyboxVertexOutput;
    output.position = uniforms.mvp * vec4<f32>(input.position, 1.0);
    output.position.z = output.position.w;
    output.ray_direction = normalize((uniforms.model * vec4<f32>(input.position, 1.0)).xyz);
    output.uv = input.uv;
    return output;
}

[Fragment]
micro fs_main(input: SkyboxVertexOutput) -> vec4<f32> {
    let dir = normalize(input.ray_direction);
    let color = sample_cubemap(skybox_cubemap, dir);
    return vec4<f32>(color, 1.0);
}

micro sample_cubemap(tex: sampler_cube, dir: vec3<f32>) -> vec3<f32> {
    return texture(tex, dir).rgb;
}
