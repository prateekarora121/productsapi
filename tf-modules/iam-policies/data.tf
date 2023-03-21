data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

data "aws_iam_policy_document" "dynamo_db_read"{
    statement {
         actions = [
                "dynamodb:BatchGet*",
                "dynamodb:DescribeStream",
                "dynamodb:DescribeTable",
                "dynamodb:Get*",
                "dynamodb:Query",
                "dynamodb:Scan",
            ]
            resources =  ["arn:aws:dynamodb:*:${data.aws_caller_identity.current.account_id}:table/${var.table_name}" , "arn:aws:dynamodb:*:*:table/${var.table_name}/*"]
    }
}

data "aws_iam_policy_document" "dynamo_db_write"{
    statement {
         actions = [
                "dynamodb:BatchWrite*",
                "dynamodb:CreateTable",
                "dynamodb:Delete*",
                "dynamodb:Update*",
                "dynamodb:PutItem"
            ]
            resources =  ["arn:aws:dynamodb:*:${data.aws_caller_identity.current.account_id}:table/${var.table_name}" , "arn:aws:dynamodb:*:*:table/${var.table_name}"]
    }
}

data "aws_iam_policy_document" "cloud_watch_put_metrics"{
    statement {
        actions = ["cloudwatch:PutMetricData"]
        resources =  ["*"]
    }
}