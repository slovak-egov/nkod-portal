import { HTMLAttributes } from 'react';

type Props = {
    count: number;
} & HTMLAttributes<HTMLDivElement>;

export default function CommentButton(props: Props) {
    return (
        <div className="govuk-!-margin-left-4 comments-button">
            <svg width="15px" height="15px" viewBox="0 0 17 17" version="1.1" xmlns="http://www.w3.org/2000/svg">
                <path
                    d="M15.5 0h-14c-0.827 0-1.5 0.673-1.5 1.5v10c0 0.827 0.673 1.5 1.5 1.5h0.5v4.102l4.688-4.102h8.812c0.827 0 1.5-0.673 1.5-1.5v-10c0-0.827-0.673-1.5-1.5-1.5zM16 11.5c0 0.275-0.224 0.5-0.5 0.5h-9.188l-3.312 2.898v-2.898h-1.5c-0.276 0-0.5-0.225-0.5-0.5v-10c0-0.275 0.224-0.5 0.5-0.5h14c0.276 0 0.5 0.225 0.5 0.5v10zM3 3h11v1h-11v-1zM3 5h11v1h-11v-1zM3 7h6v1h-6v-1z"
                    fill="#000000"
                />
            </svg>
            <span className="govuk-!-padding-left-1 govuk-!-font-weight-bold">{props.count}</span>
        </div>
    );
}

CommentButton.defaultProps = {
    count: 0
};
