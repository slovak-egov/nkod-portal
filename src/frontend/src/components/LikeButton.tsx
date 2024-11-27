import classNames from 'classnames';
import { HTMLAttributes, useState } from 'react';
import { useUserPermissions } from '../client';
import { useCmsLike } from '../cms';

type Props = {
    count: number;
    url: string;
    contentId?: string;
    datasetUri?: string;
} & HTMLAttributes<HTMLDivElement>;

export default function LikeButton(props: Props) {
    const { count, url, contentId, datasetUri } = props;
    const { isLogged } = useUserPermissions();
    const [likeLoading, likeError, like] = useCmsLike();
    const [likeCount, setLikeCount] = useState(count);

    return (
        <div
            className={classNames({ 'like-button': isLogged }, 'govuk-!-padding-left-3')}
            onClick={async () => {
                if (isLogged) {
                    const response = await like(url, contentId, datasetUri);
                    if (response.success) {
                        setLikeCount(response.newLikeCount ?? 0);
                    }
                }
            }}
        >
            <svg fill="#000000" height="15px" width="15px" version="1.1" id="Layer_1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
                <g>
                    <g>
                        <path
                            d="M83.478,139.13H16.696C7.479,139.13,0,146.603,0,155.826v333.913c0,9.223,7.479,16.696,16.696,16.696h66.783
			c9.217,0,16.696-7.473,16.696-16.696V155.826C100.174,146.603,92.695,139.13,83.478,139.13z M50.089,439.652
			c-9.212,0-16.696-7.484-16.696-16.696s7.484-16.696,16.696-16.696s16.696,7.484,16.696,16.696S59.302,439.652,50.089,439.652z"
                        />
                    </g>
                </g>
                <g>
                    <g>
                        <path
                            d="M512,222.609c0-27.619-22.468-50.087-50.087-50.087H334.054c21.989-51.386,18.391-114.603,18.228-116.87
			c0-27.619-22.468-50.087-50.087-50.087s-50.087,22.468-50.087,50.087c0,44.07-26.75,84.749-66.554,101.222l-41.673,17.233
			c-6.24,2.581-10.316,8.674-10.316,15.429v266.81c0,9.223,7.479,16.696,16.696,16.696h211.478
			c27.619,0,50.087-22.468,50.087-50.087c0-6.511-1.25-12.739-3.522-18.451c21.25-5.799,36.913-25.272,36.913-48.332
			c0-6.511-1.25-12.739-3.522-18.451c21.25-5.799,36.913-25.272,36.913-48.332c0-6.511-1.25-12.739-3.522-18.45
			C496.337,265.142,512,245.669,512,222.609z"
                        />
                    </g>
                </g>
            </svg>
            <span className="govuk-!-padding-left-1 govuk-!-font-weight-bold">{likeCount}</span>
        </div>
    );
}

LikeButton.defaultProps = {
    count: 0,
    url: null,
    contentId: null,
    dataset: null
};
