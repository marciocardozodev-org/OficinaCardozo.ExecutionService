terraform {
  backend "s3" {
    bucket  = "oficina-cardozo-terraform-state-sp"
    key     = "db/prod/terraform.tfstate"
    region  = "sa-east-1"
    encrypt = true
  }
}