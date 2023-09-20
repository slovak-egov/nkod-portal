import { HTMLAttributes, InputHTMLAttributes } from "react";

interface IProps extends InputHTMLAttributes<HTMLInputElement>
{
    
}

export default function FileUpload(props: IProps)
{
    return <input className="govuk-file-upload" type="file" {...props} />
}