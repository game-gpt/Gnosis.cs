using gg_shader::f32::{vec2, vec3, vec4, mat4}
using gg_shader::texture_cube
using gg_shader::sampler

shader skybox {
    uniform mvp: mat4,
    uniform model: mat4,

    texture skybox_cubemap: texture_cube,
    sampler cube_sampler,

    varying v_uv: vec2,
    varying v_ray_direction: vec3,

    [vertex_main]
    vs_main(position: vec3, normal: vec3, uv: vec2) -> vec4 {
        let clip_pos = mvp * vec4(position, 1.0)
        v_uv = uv
        v_ray_direction = normalize((model * vec4(position, 1.0)).xyz)
        let mut output_pos = clip_pos
        output_pos.z = clip_pos.w
        return output_pos
    }

    [fragment_main]
    fs_main() -> vec4 {
        let dir = normalize(v_ray_direction)
        let color = sample_cubemap(dir)
        return vec4(color, 1.0)
    }

    micro sample_cubemap(dir: vec3) -> vec3 {
        return texture(skybox_cubemap, cube_sampler, dir).rgb
    }
}
