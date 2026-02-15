# RDS PostgreSQL Free Tier
resource "aws_db_instance" "main" {
  allocated_storage    = 20
  engine               = "postgres"
  engine_version       = "15.4"
  instance_class       = "db.t3.micro"
  db_name              = var.db_name
  username             = var.db_username
  password             = var.db_password
  parameter_group_name = "default.postgres15"
  db_subnet_group_name = aws_db_subnet_group.main.name
  vpc_security_group_ids = var.db_security_group_ids
  skip_final_snapshot  = true
  publicly_accessible  = false
  storage_encrypted    = true
  tags = {
    Name = "${var.app_name}-rds-postgres"
  }
}

resource "aws_db_subnet_group" "main" {
  name       = "${var.app_name}-rds-subnet-group"
  subnet_ids = var.db_subnet_ids
  tags = {
    Name = "${var.app_name}-rds-subnet-group"
  }
}