# RDS PostgreSQL Free Tier
resource "aws_db_instance" "billingservice" {
  allocated_storage    = 20
  engine               = "postgres"
  instance_class       = "db.t3.micro"
  db_name              = var.billingservice_db_name
  username             = var.billingservice_db_username
  password             = var.billingservice_db_password
  # parameter_group_name removido para usar o padrão da AWS
  db_subnet_group_name = aws_db_subnet_group.billingservice.name
  vpc_security_group_ids = var.billingservice_db_security_group_ids
  skip_final_snapshot  = true
  publicly_accessible  = false
  storage_encrypted    = true
  tags = {
    Name = "${var.billingservice_app_name}-rds-postgres"
  }
}

resource "aws_db_subnet_group" "billingservice" {
  name       = "${var.billingservice_app_name}-rds-subnet-group"
  subnet_ids = var.billingservice_db_subnet_ids
  tags = {
    Name = "${var.billingservice_app_name}-rds-subnet-group"
  }
}