struct TerrainVertexInput {
    position: vec3<f32>,
    normal: vec3<f32>,
    uv: vec2<f32>,
}

struct TerrainVertexOutput {
    position: vec4<f32>,
    normal: vec3<f32>,
    uv: vec2<f32>,
    world_pos: vec3<f32>,
}

[Vertex]
micro vs_main(input: TerrainVertexInput) -> TerrainVertexOutput {
    let mut output: TerrainVertexOutput;
    output.position = uniforms.mvp * vec4<f32>(input.position, 1.0);
    output.normal = normalize((uniforms.model * vec4<f32>(input.normal, 0.0)).xyz);
    output.uv = input.uv;
    output.world_pos = (uniforms.model * vec4<f32>(input.position, 1.0)).xyz;
    return output;
}

[Fragment]
micro fs_main(input: TerrainVertexOutput) -> vec4<f32> {
    let normal = normalize(input.normal);
    let height = input.world_pos.y;

    let rock_color = vec3<f32>(0.4, 0.35, 0.3);
    let grass_color = vec3<f32>(0.2, 0.5, 0.15);
    let snow_color = vec3<f32>(0.9, 0.9, 0.95);
    let sand_color = vec3<f32>(0.76, 0.7, 0.5);

    let slope = 1.0 - max(dot(normal, vec3<f32>(0.0, 1.0, 0.0)), 0.0);

    let mut base_color: vec3<f32>;
    if height < 2.0 {
        base_color = sand_color;
    } else if height < 30.0 {
        let t = (height - 2.0) / 28.0;
        base_color = mix(sand_color, grass_color, t);
    } else if height < 60.0 {
        let t = (height - 30.0) / 30.0;
        base_color = mix(grass_color, rock_color, t);
    } else {
        let t = min((height - 60.0) / 20.0, 1.0);
        base_color = mix(rock_color, snow_color, t);
    }

    if slope > 0.5 {
        let t = min((slope - 0.5) / 0.3, 1.0);
        base_color = mix(base_color, rock_color, t);
    }

    let light_dir = normalize(vec3<f32>(0.5, 1.0, 0.3));
    let ambient = 0.3;
    let diffuse = max(dot(normal, light_dir), 0.0) * 0.7;
    let lighting = ambient + diffuse;

    return vec4<f32>(base_color * lighting, 1.0);
}
