using gg_shader::f32::{vec2, vec3, vec4, mat4}

shader grass {
    uniform mvp: mat4,
    uniform model: mat4,

    cbuffer grass {
        wind_direction: vec3,
        wind_speed: f32,
        wind_strength: f32,
        time: f32,
    },

    varying v_normal: vec3,
    varying v_uv: vec2,
    varying v_color: vec3,

    [vertex_main]
    vs_main(position: vec3, normal: vec3, uv: vec2,
            instance_pos: vec3, instance_scale: f32, instance_rotation: vec3,
            instance_color: u32, instance_type: i32) -> vec4 {
        let scale = instance_scale
        let rotation_y = instance_rotation.y * 3.14159265 / 180.0

        let cos_r = cos(rotation_y)
        let sin_r = sin(rotation_y)
        let rotated_x = position.x * cos_r - position.z * sin_r
        let rotated_z = position.x * sin_r + position.z * cos_r

        let world_pos = vec3(
            rotated_x * scale + instance_pos.x,
            position.y * scale + instance_pos.y,
            rotated_z * scale + instance_pos.z
        )

        let wind_offset = compute_wind(world_pos, uv.y)

        let final_pos = world_pos + wind_offset

        v_normal = normalize((model * vec4(normal, 0.0)).xyz)
        v_uv = uv
        v_color = unpack_color(instance_color)

        return mvp * vec4(final_pos, 1.0)
    }

    [fragment_main]
    fs_main() -> vec4 {
        let normal = normalize(v_normal)
        let light_dir = normalize(vec3(0.5, 1.0, 0.3))

        let ambient = 0.4
        let diffuse = max(dot(normal, light_dir), 0.0) * 0.6
        let lighting = ambient + diffuse

        let height_fade = smoothstep(0.0, 0.2, v_uv.y)
        let alpha = height_fade

        return vec4(v_color * lighting, alpha)
    }

    micro compute_wind(pos: vec3, uv_y: f32) -> vec3 {
        let wind_dir = normalize(grass.wind_direction)
        let time = grass.time * grass.wind_speed
        let wind_phase = pos.x * 0.1 + pos.z * 0.1 + time
        let wind_strength = sin(wind_phase) * grass.wind_strength * uv_y * uv_y
        return wind_dir * wind_strength
    }

    micro unpack_color(packed: u32) -> vec3 {
        let r = f32(packed & 0xFFu) / 255.0
        let g = f32((packed >> 8u) & 0xFFu) / 255.0
        let b = f32((packed >> 16u) & 0xFFu) / 255.0
        return vec3(r, g, b)
    }
}
