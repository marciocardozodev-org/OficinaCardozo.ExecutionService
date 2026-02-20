# ============================================================================
# ExecutionService - SQS Queue Access Policies para SNS→SQS Integration
# ============================================================================

# Data source para obter fila SQS billing-events
data "aws_sqs_queue" "billing_events" {
  name = "billing-events"
}

# Data source para obter fila SQS execution-events
data "aws_sqs_queue" "execution_events" {
  name = "execution-events"
}

# ============================================================================
# SNS→SQS Subscriptions
# ============================================================================

resource "aws_sns_topic_subscription" "execution_started_to_billing_events" {
  topic_arn            = aws_sns_topic.execution_started.arn
  protocol             = "sqs"
  endpoint             = data.aws_sqs_queue.billing_events.url
  raw_message_delivery = true
}

resource "aws_sns_topic_subscription" "execution_finished_to_billing_events" {
  topic_arn            = aws_sns_topic.execution_finished.arn
  protocol             = "sqs"
  endpoint             = data.aws_sqs_queue.billing_events.url
  raw_message_delivery = true
}

resource "aws_sns_topic_subscription" "execution_events_to_execution_events_queue" {
  topic_arn            = aws_sns_topic.execution_events.arn
  protocol             = "sqs"
  endpoint             = data.aws_sqs_queue.execution_events.url
  raw_message_delivery = true
}

# ============================================================================
# SQS Queue Policies - Allow SNS to send messages
# ============================================================================

# Policy: SNS pode enviar mensagens à fila billing-events
resource "aws_sqs_queue_policy" "billing_events_allow_execution_sns" {
  queue_url = data.aws_sqs_queue.billing_events.url

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowExecutionSNSPublish"
        Effect = "Allow"
        Principal = {
          Service = "sns.amazonaws.com"
        }
        Action   = "sqs:SendMessage"
        Resource = data.aws_sqs_queue.billing_events.arn
        Condition = {
          ArnEquals = {
            "aws:SourceArn" = [
              aws_sns_topic.execution_started.arn,
              aws_sns_topic.execution_finished.arn
            ]
          }
        }
      }
    ]
  })
}

# Policy: SNS pode enviar mensagens à fila execution-events
resource "aws_sqs_queue_policy" "execution_events_allow_execution_sns" {
  queue_url = data.aws_sqs_queue.execution_events.url

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowExecutionSNSPublish"
        Effect = "Allow"
        Principal = {
          Service = "sns.amazonaws.com"
        }
        Action   = "sqs:SendMessage"
        Resource = data.aws_sqs_queue.execution_events.arn
        Condition = {
          ArnEquals = {
            "aws:SourceArn" = [
              aws_sns_topic.execution_events.arn
            ]
          }
        }
      }
    ]
  })
}

# ============================================================================
# Outputs
# ============================================================================

output "billing_events_queue_arn" {
  value       = data.aws_sqs_queue.billing_events.arn
  description = "ARN da fila SQS billing-events"
}

output "execution_events_queue_arn" {
  value       = data.aws_sqs_queue.execution_events.arn
  description = "ARN da fila SQS execution-events"
}

output "execution_started_subscription_arn" {
  value       = aws_sns_topic_subscription.execution_started_to_billing_events.arn
  description = "ARN da subscription execution-started → billing-events"
}

output "execution_finished_subscription_arn" {
  value       = aws_sns_topic_subscription.execution_finished_to_billing_events.arn
  description = "ARN da subscription execution-finished → billing-events"
}

output "execution_events_subscription_arn" {
  value       = aws_sns_topic_subscription.execution_events_to_execution_events_queue.arn
  description = "ARN da subscription execution-events → execution-events"
}
