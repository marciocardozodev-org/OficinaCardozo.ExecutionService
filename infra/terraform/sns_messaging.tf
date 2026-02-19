# ============================================================================
# ExecutionService - SNS Topics para publicação de eventos
# ============================================================================

# Tópico SNS para publicar eventos de execução
resource "aws_sns_topic" "execution_events" {
  name              = "execution-events"
  display_name      = "Execution Service Events"
  fifo_topic        = false
  
  tags = {
    Service     = "ExecutionService"
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}

# Política de acesso ao tópico SNS (permite publicação)
resource "aws_sns_topic_policy" "execution_events" {
  arn = aws_sns_topic.execution_events.arn

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowExecutionServicePublish"
        Effect = "Allow"
        Principal = {
          AWS = "*"
        }
        Action = [
          "SNS:Publish",
          "SNS:Subscribe"
        ]
        Resource = aws_sns_topic.execution_events.arn
        Condition = {
          StringEquals = {
            "AWS:SourceOwner" = data.aws_caller_identity.current.account_id
          }
        }
      }
    ]
  })
}

# Data source para obter account ID
data "aws_caller_identity" "current" {}

# Outputs
output "execution_events_topic_arn" {
  value       = aws_sns_topic.execution_events.arn
  description = "ARN do tópico SNS execution-events"
}

output "execution_events_topic_name" {
  value       = aws_sns_topic.execution_events.name
  description = "Nome do tópico SNS execution-events"
}
