extends AnimatedSprite2D

func play_move(direction: String) -> void:
    play("walk_" + direction)

func play_idle(direction: String) -> void:
    play("idle_" + direction)
