import { HTMLAttributes } from "react";

interface IProps extends HTMLAttributes<HTMLTextAreaElement>
{
    
}

export default function TextArea(props: IProps)
{
    return <textarea className="govuk-textarea" {...props} />;
}

TextArea.defaultProps = {
    rows: 5
}