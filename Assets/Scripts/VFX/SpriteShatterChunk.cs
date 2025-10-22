using UnityEngine;

namespace QixLike
{
    public class SpriteShatterChunk : MonoBehaviour
    {
        Vector2 vel;
        float gravity;
        float life;
        float angularVel;
        float t;
        SpriteRenderer sr;

        // �X���[���[�V�����̉e�����󂯂��ɐi�߂����̂� unscaled ���̗p
        const bool UseUnscaled = true;

        public void Init(Vector2 initialVel, float gravityAccel, float lifetime, float angVel)
        {
            vel = initialVel;
            gravity = gravityAccel;
            life = lifetime;
            angularVel = angVel;
            sr = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            float dt = UseUnscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            // �ȈՕ���
            vel.y -= gravity * dt;
            transform.position += (Vector3)(vel * dt);
            transform.Rotate(0f, 0f, angularVel * dt);

            // �t�F�[�h�A�E�g
            if (sr)
            {
                var c = sr.color;
                c.a = Mathf.Clamp01(1f - (t / life));
                sr.color = c;
            }

            if (t >= life) Destroy(gameObject);
        }
    }
}
