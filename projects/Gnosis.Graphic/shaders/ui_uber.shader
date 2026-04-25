struct UiVertexInput {
    position: vec3<f32>,
    color: vec4<f32>,
    uv: vec2<f32>,
}

struct UiVertexOutput {
    position: vec4<f32>,
    color: vec4<f32>,
    uv: vec2<f32>,
}

cbuffer UiShaderParams {
    sdf_spread: f32,
    outline_width: f32,
    outline_color: vec4<f32>,
    shadow_offset: vec2<f32>,
    shadow_width: f32,
    shadow_color: vec4<f32>,
    style_id: i32,
    smoothness: f32,
}

texture sdf_atlas: texture2D
sampler atlas_sampler

[Vertex]
micro vs_main(input: UiVertexInput) -> UiVertexOutput {
    let mut output: UiVertexOutput;
    output.position = vec4<f32>(input.position, 1.0);
    output.color = input.color;
    output.uv = input.uv;
    return output;
}

[Fragment]
micro fs_main(input: UiVertexOutput) -> vec4<f32> {
    let dist = sample_sdf(input.uv);

    let edge = 0.5;
    let smooth_edge = max(smoothness, 0.001);

    let fill_alpha = smoothstep(edge - smooth_edge, edge + smooth_edge, dist);

    let outline_alpha = compute_outline(dist, edge, smooth_edge, outline_width, sdf_spread);

    let shadow_alpha = compute_shadow(input.uv, edge, smooth_edge, shadow_width, sdf_spread, shadow_offset);

    let out_r = shadow_color.r * shadow_alpha * shadow_color.a
              + outline_color.r * outline_alpha
              + input.color.r * fill_alpha * input.color.a;
    let out_g = shadow_color.g * shadow_alpha * shadow_color.a
              + outline_color.g * outline_alpha
              + input.color.g * fill_alpha * input.color.a;
    let out_b = shadow_color.b * shadow_alpha * shadow_color.a
              + outline_color.b * outline_alpha
              + input.color.b * fill_alpha * input.color.a;
    let out_a = min(shadow_alpha * shadow_color.a + outline_alpha + fill_alpha * input.color.a, 1.0);

    return vec4<f32>(out_r, out_g, out_b, out_a);
}

micro sample_sdf(uv: vec2<f32>) -> f32 {
    let sampled = texture(sdf_atlas, atlas_sampler, uv);
    return sampled.r;
}

micro compute_outline(dist: f32, edge: f32, smooth_edge: f32, width: f32, spread: f32) -> f32 {
    if width <= 0.0 {
        return 0.0;
    }

    let outer_edge = edge + width / spread;
    let inner = smoothstep(edge - smooth_edge, edge + smooth_edge, dist);
    let outer = smoothstep(outer_edge - smooth_edge, outer_edge + smooth_edge, dist);
    return inner * (1.0 - outer);
}

micro compute_shadow(uv: vec2<f32>, edge: f32, smooth_edge: f32, width: f32, spread: f32, offset: vec2<f32>) -> f32 {
    if width <= 0.0 {
        return 0.0;
    }

    let shadow_uv = uv - offset;
    let shadow_dist = sample_sdf(shadow_uv);
    let shadow_edge = edge - width / spread;
    return smoothstep(shadow_edge - smooth_edge, shadow_edge + smooth_edge, shadow_dist);
}
