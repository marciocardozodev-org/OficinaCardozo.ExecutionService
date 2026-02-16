# Teste de modificação automática - 16/02/2026
# RDS PostgreSQL Free Tier
resource "aws_db_instance" "osservice" {
  allocated_storage    = 20
  engine               = "postgres"
  instance_class       = "db.t3.micro"
  db_name              = var.osservice_db_name
  username             = var.osservice_db_username
  password             = var.osservice_db_password
  # parameter_group_name removido para usar o padrão da AWS
  db_subnet_group_name = aws_db_subnet_group.osservice.name
  vpc_security_group_ids = var.osservice_db_security_group_ids
  skip_final_snapshot  = true
  publicly_accessible  = false
  storage_encrypted    = true
  tags = {
    Name = "${var.osservice_app_name}-rds-postgres"
  }
}

resource "aws_db_subnet_group" "osservice" {
  name       = "${var.osservice_app_name}-rds-subnet-group"
  subnet_ids = var.osservice_db_subnet_ids
  tags = {
    Name = "${var.osservice_app_name}-rds-subnet-group"
  }
}