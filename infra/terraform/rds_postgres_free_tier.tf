# RDS PostgreSQL Free Tier
resource "aws_db_instance" "executionservice" {
  identifier              = var.executionservice_rds_instance_id
  allocated_storage       = 20
  engine                  = "postgres"
  instance_class          = "db.t3.micro"
  db_name                 = var.executionservice_db_name
  username                = var.executionservice_db_username
  password                = var.executionservice_db_password
  # parameter_group_name removido para usar o padrão da AWS
  db_subnet_group_name    = aws_db_subnet_group.executionservice.name
  vpc_security_group_ids  = var.executionservice_db_security_group_ids
  skip_final_snapshot     = true
  publicly_accessible     = false
  storage_encrypted       = true
  tags = {
    Name = "${var.executionservice_app_name}-rds-postgres"
  }
}

resource "aws_db_subnet_group" "executionservice" {
  name       = "${var.executionservice_app_name}-rds-subnet-group"
  subnet_ids = var.executionservice_db_subnet_ids
  tags = {
    Name = "${var.executionservice_app_name}-rds-subnet-group"
  }
}