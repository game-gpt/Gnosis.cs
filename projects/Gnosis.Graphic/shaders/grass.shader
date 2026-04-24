struct GrassVertexInput {
    position: vec3<f32>,
    normal: vec3<f32>,
    uv: vec2<f32>,
}

struct GrassInstanceData {
    instance_pos: vec3<f32>,
    instance_scale: f32,
    instance_rotation: vec3<f32>,
    instance_color: u32,
    instance_type: i32,
}

struct GrassVertexOutput {
    position: vec4<f32>,
    normal: vec3<f32>,
    uv: vec2<f32>,
    color: vec3<f32>,
    world_pos: vec3<f32>,
}

[Vertex]
micro vs_main(input: GrassVertexInput, instance: GrassInstanceData) -> GrassVertexOutput {
    let mut output: GrassVertexOutput;

    let scale = instance.instance_scale;
    let rotation_y = instance.instance_rotation.y * 3.14159265 / 180.0;

    let cos_r = cos(rotation_y);
    let sin_r = sin(rotation_y);
    let rotated_x = input.position.x * cos_r - input.position.z * sin_r;
    let rotated_z = input.position.x * sin_r + input.position.z * cos_r;

    let world_pos = vec3<f32>(
        rotated_x * scale + instance.instance_pos.x,
        input.position.y * scale + instance.instance_pos.y,
        rotated_z * scale + instance.instance_pos.z
    );

    let wind_offset = compute_wind(world_pos, input.uv.y);

    let final_pos = world_pos + wind_offset;

    output.position = uniforms.mvp * vec4<f32>(final_pos, 1.0);
    output.normal = normalize((uniforms.model * vec4<f32>(input.normal, 0.0)).xyz);
    output.uv = input.uv;
    output.color = unpack_color(instance.instance_color);
    output.world_pos = final_pos;

    return output;
}

[Fragment]
micro fs_main(input: GrassVertexOutput) -> vec4<f32> {
    let normal = normalize(input.normal);
    let light_dir = normalize(vec3<f32>(0.5, 1.0, 0.3));

    let ambient = 0.4;
    let diffuse = max(dot(normal, light_dir), 0.0) * 0.6;
    let lighting = ambient + diffuse;

    let height_fade = smoothstep(0.0, 0.2, input.uv.y);
    let base_alpha = 1.0;
    let alpha = base_alpha * height_fade;

    return vec4<f32>(input.color * lighting, alpha);
}

micro compute_wind(pos: vec3<f32>, uv_y: f32) -> vec3<f32> {
    let wind_dir = normalize(grass.wind_direction);
    let time = grass.time * grass.wind_speed;
    let wind_phase = pos.x * 0.1 + pos.z * 0.1 + time;
    let wind_strength = sin(wind_phase) * grass.wind_strength * uv_y * uv_y;
    return wind_dir * wind_strength;
}

micro unpack_color(packed: u32) -> vec3<f32> {
    let r = f32(packed & 0xFFu) / 255.0;
    let g = f32((packed >> 8u) & 0xFFu) / 255.0;
    let b = f32((packed >> 16u) & 0xFFu) / 255.0;
    return vec3<f32>(r, g, b);
}
